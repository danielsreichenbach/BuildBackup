﻿using Microsoft.Extensions.Configuration;

namespace BuildBackup
{
    public static class SettingsManager
    {
        public static string cacheDir;
        public static string[] checkProducts;
        public static string[] backupProducts;
        public static bool useRibbit;
        public static bool downloadPatchFiles;
        public static int maxParallelDownloads;

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            var config = new ConfigurationBuilder().AddJsonFile("config.json", optional: false, reloadOnChange: false).Build();
            cacheDir = config.GetSection("config").GetSection("cacheDir").Get<string>();
            checkProducts = config.GetSection("config").GetSection("checkProducts").Get<string[]>();
            backupProducts = config.GetSection("config").GetSection("backupProducts").Get<string[]>();
            useRibbit = config.GetSection("config").GetSection("useRibbit").Get<bool>();
            downloadPatchFiles = config.GetSection("config").GetSection("downloadPatchFiles").Get<bool>();
            maxParallelDownloads = config.GetSection("config").GetSection("maxParallelDownloads").Get<int>();
            
            // Default to 4 parallel downloads if not specified or invalid
            if (maxParallelDownloads <= 0)
                maxParallelDownloads = 4;
        }
    }
}
