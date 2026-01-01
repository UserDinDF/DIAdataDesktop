using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DIAdataDesktop.Data
{
    public sealed class FavoritesRepository
    {
        private readonly string _dbPath;
        private readonly string _cs;

        public FavoritesRepository(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));

            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            _cs = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        public async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            await using var con = new SqliteConnection(_cs);
            await con.OpenAsync(ct);

            await using var cmd = con.CreateCommand();
            cmd.CommandText =
@"
CREATE TABLE IF NOT EXISTS favorites (
    kind     TEXT NOT NULL,   -- 'token' | 'exchange'
    key      TEXT NOT NULL,   -- normalized unique key (WITHOUT kind)
    name     TEXT,
    extra1   TEXT,
    extra2   TEXT,
    added_at TEXT NOT NULL,
    PRIMARY KEY (kind, key)
);

CREATE INDEX IF NOT EXISTS ix_favorites_kind ON favorites(kind);
";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static string Norm(string? s) => (s ?? "").Trim().ToLowerInvariant();

        // ✅ key without kind (kind is its own column)
        public static string MakeTokenKey(string? blockchain, string? address)
            => $"{Norm(blockchain)}|{Norm(address)}";

        public static string MakeExchangeKey(string? name)
            => Norm(name);

        public async Task<HashSet<string>> GetKeysAsync(string kind, CancellationToken ct = default)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await using var con = new SqliteConnection(_cs);
            await con.OpenAsync(ct);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT key FROM favorites WHERE kind = $kind;";
            cmd.Parameters.AddWithValue("$kind", kind);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                set.Add(r.GetString(0));

            return set;
        }

        public async Task UpsertAsync(
            string kind,
            string key,
            string? name,
            string? extra1,
            string? extra2,
            CancellationToken ct = default)
        {
            await using var con = new SqliteConnection(_cs);
            await con.OpenAsync(ct);

            await using var cmd = con.CreateCommand();
            cmd.CommandText =
@"
INSERT OR REPLACE INTO favorites (kind, key, name, extra1, extra2, added_at)
VALUES ($kind, $key, $name, $e1, $e2, $at);
";
            cmd.Parameters.AddWithValue("$kind", kind);
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$name", name ?? "");
            cmd.Parameters.AddWithValue("$e1", extra1 ?? "");
            cmd.Parameters.AddWithValue("$e2", extra2 ?? "");
            cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task RemoveAsync(string kind, string key, CancellationToken ct = default)
        {
            await using var con = new SqliteConnection(_cs);
            await con.OpenAsync(ct);

            await using var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM favorites WHERE kind = $kind AND key = $key;";
            cmd.Parameters.AddWithValue("$kind", kind);
            cmd.Parameters.AddWithValue("$key", key);

            await cmd.ExecuteNonQueryAsync(ct);
        }

    }
}
