using DIAdataDesktop.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace DIAdataDesktop.Data
{
    public sealed class AppDb
    {
        private readonly string _dbPath;
        private readonly object _gate = new();

        public AppDb(string dbPath)
        {
            _dbPath = dbPath;
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            EnsureCreated();
        }

        private string Cs => new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        private void EnsureCreated()
        {
            lock (_gate)
            {
                using var con = new SqliteConnection(Cs);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandText = """
                PRAGMA journal_mode=WAL;

                CREATE TABLE IF NOT EXISTS quotes(
                    symbol TEXT NOT NULL PRIMARY KEY,
                    name TEXT NULL,
                    address TEXT NULL,
                    blockchain TEXT NULL,
                    price REAL NOT NULL,
                    priceYesterday REAL NOT NULL,
                    volumeYesterdayUsd REAL NOT NULL,
                    time TEXT NOT NULL,
                    source TEXT NULL,
                    savedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS watchlist(
                    symbol TEXT NOT NULL PRIMARY KEY
                );
                """;
                cmd.ExecuteNonQuery();
            }
        }

        public void UpsertQuote(DiaQuotation q)
        {
            lock (_gate)
            {
                using var con = new SqliteConnection(Cs);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandText = """
                INSERT INTO quotes(symbol,name,address,blockchain,price,priceYesterday,volumeYesterdayUsd,time,source,savedAt)
                VALUES($symbol,$name,$address,$blockchain,$price,$py,$vol,$time,$source,$savedAt)
                ON CONFLICT(symbol) DO UPDATE SET
                    name=excluded.name,
                    address=excluded.address,
                    blockchain=excluded.blockchain,
                    price=excluded.price,
                    priceYesterday=excluded.priceYesterday,
                    volumeYesterdayUsd=excluded.volumeYesterdayUsd,
                    time=excluded.time,
                    source=excluded.source,
                    savedAt=excluded.savedAt;
                """;
                cmd.Parameters.AddWithValue("$symbol", q.Symbol ?? "");
                cmd.Parameters.AddWithValue("$name", (object?)q.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$address", (object?)q.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$blockchain", (object?)q.Blockchain ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$price", q.Price);
                cmd.Parameters.AddWithValue("$py", q.PriceYesterday);
                cmd.Parameters.AddWithValue("$vol", q.VolumeYesterdayUSD);
                cmd.Parameters.AddWithValue("$time", q.Time.ToString("O"));
                cmd.Parameters.AddWithValue("$source", (object?)q.Source ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$savedAt", DateTimeOffset.UtcNow.ToString("O"));
                cmd.ExecuteNonQuery();
            }
        }

        public DiaQuotation? GetLastQuote(string symbol)
        {
            symbol = (symbol ?? "").Trim().ToUpperInvariant();
            if (symbol.Length == 0) return null;

            lock (_gate)
            {
                using var con = new SqliteConnection(Cs);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandText = """
                SELECT symbol,name,address,blockchain,price,priceYesterday,volumeYesterdayUsd,time,source
                FROM quotes WHERE symbol=$symbol;
                """;
                cmd.Parameters.AddWithValue("$symbol", symbol);

                using var r = cmd.ExecuteReader();
                if (!r.Read()) return null;

                return new DiaQuotation
                {
                    Symbol = r.GetString(0),
                    Name = r.IsDBNull(1) ? null : r.GetString(1),
                    Address = r.IsDBNull(2) ? null : r.GetString(2),
                    Blockchain = r.IsDBNull(3) ? null : r.GetString(3),
                    Price = r.GetDouble(4),
                    PriceYesterday = r.GetDouble(5),
                    VolumeYesterdayUSD = r.GetDouble(6),
                    Time = DateTimeOffset.Parse(r.GetString(7)),
                    Source = r.IsDBNull(8) ? null : r.GetString(8),
                };
            }
        }

        public void AddToWatchlist(string symbol)
        {
            symbol = (symbol ?? "").Trim().ToUpperInvariant();
            if (symbol.Length == 0) return;

            lock (_gate)
            {
                using var con = new SqliteConnection(Cs);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO watchlist(symbol) VALUES($s);";
                cmd.Parameters.AddWithValue("$s", symbol);
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> LoadWatchlist()
        {
            lock (_gate)
            {
                using var con = new SqliteConnection(Cs);
                con.Open();

                using var cmd = con.CreateCommand();
                cmd.CommandText = "SELECT symbol FROM watchlist ORDER BY symbol;";
                using var r = cmd.ExecuteReader();

                var list = new List<string>();
                while (r.Read())
                    list.Add(r.GetString(0));
                return list;
            }
        }
    }
}
