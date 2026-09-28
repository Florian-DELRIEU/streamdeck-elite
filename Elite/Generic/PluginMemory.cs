using System;
using System.IO;
using System.Text;
using System.Threading;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// File of what the plugin remembers between two sessions (v3.3, docs/L10-v3.md):
    /// %APPDATA%\ZV Stream Deck Elite\memoire.json, outside of the plugin folder so that it survives a reinstallation.
    /// Sections: ships (ShipMemory) and history (History). Written atomically (temporary file, then replaced), at most
    /// every SaveInterval; an unreadable file is ignored (WARN) and rewritten. Without Configure (unit tests): memory only.
    /// </summary>
    public static class PluginMemory
    {
        public static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(30);
        public const int Version = 1;

        private static readonly object Sync = new object();
        private static string path;
        private static DateTime lastSave = DateTime.MinValue;
        private static Timer pendingSave;

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ZV Stream Deck Elite", "memoire.json");
            }
        }

        /// <summary>Reads the file (if any) and gives its sections to ShipMemory and History.</summary>
        public static void Configure(string file)
        {
            JObject json = null;
            try
            {
                if (File.Exists(file))
                    json = JObject.Parse(File.ReadAllText(file, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Log(TracingLevel.WARN, $"PluginMemory: {file} ignored (unreadable): {ex.Message}");
            }

            lock (Sync)
                path = file;

            ShipMemory.Load(json?["ships"] as JObject, (string)json?["currentShip"]);
            History.Load(json?["history"] as JObject, DateTime.UtcNow);
            Log(TracingLevel.INFO, $"PluginMemory: {file} ({ShipMemory.ShipCount} ships, {History.SeriesCount} history series)");
        }

        /// <summary>For unit tests: no file, nothing remembered.</summary>
        internal static void Reset()
        {
            lock (Sync)
            {
                path = null;
                pendingSave?.Dispose();
                pendingSave = null;
                lastSave = DateTime.MinValue;
            }

            ShipMemory.Reset();
            History.Reset();
        }

        /// <summary>Saves now if the last save is old enough, otherwise once at the end of the interval.</summary>
        public static void RequestSave()
        {
            lock (Sync)
            {
                if (path == null || pendingSave != null)
                    return;

                var wait = lastSave + SaveInterval - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    pendingSave = new Timer(_ => SaveNow(), null, wait, Timeout.InfiniteTimeSpan);
                    return;
                }
            }

            SaveNow();
        }

        public static void SaveNow()
        {
            string file;
            lock (Sync)
            {
                file = path;
                pendingSave?.Dispose();
                pendingSave = null;
                lastSave = DateTime.UtcNow;
            }

            if (file == null)
                return;

            try
            {
                var json = new JObject
                {
                    { "version", Version },
                    { "currentShip", ShipMemory.CurrentShipId },
                    { "ships", ShipMemory.Snapshot() },
                    { "history", History.Snapshot() },
                };

                Directory.CreateDirectory(Path.GetDirectoryName(file));
                var temporary = file + ".tmp";
                File.WriteAllText(temporary, json.ToString(Formatting.Indented), new UTF8Encoding(false));
                if (File.Exists(file))
                    File.Replace(temporary, file, null);
                else
                    File.Move(temporary, file);
            }
            catch (Exception ex)
            {
                Log(TracingLevel.ERROR, $"PluginMemory: cannot write {file}: {ex}");
            }
        }

        private static void Log(TracingLevel level, string message)
        {
            try
            {
                Logger.Instance.LogMessage(level, message);
            }
            catch (Exception)
            {
                // unit tests without the Stream Deck logger
            }
        }
    }
}
