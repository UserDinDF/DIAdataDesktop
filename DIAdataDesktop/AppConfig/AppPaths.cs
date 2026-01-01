using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DIAdataDesktop.AppConfig
{
    public static class AppPaths
    {
        public static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

        public static string RwaIconPath(string appSlug) => Path.Combine(BaseDir, "Logos", "RWAs", $"{appSlug}.png");
    }
}
