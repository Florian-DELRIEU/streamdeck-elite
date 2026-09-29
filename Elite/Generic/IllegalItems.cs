using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Backpack items that the plugin counts as illegal (v3.8, docs/L11-v3.8.md, D10: status.LegalState only follows the
    /// ship, and the journal does not say whether an item is illegal). Editable list, file objets-illegaux.json next to
    /// memoire.json (%APPDATA%\ZV Stream Deck Elite), created with the defaults, read when the plugin starts.
    /// Names = internal names of the game, those of the calc.Backpack.&lt;name&gt; keys.
    /// </summary>
    public static class IllegalItems
    {
        public const string FileName = "objets-illegaux.json";
        public const string ListField = "objets";

        /// <summary>E-Breach ("Omnipasse"), the item of the D10 test.</summary>
        public static readonly string[] Defaults = { "bypass" };

        private static readonly object Sync = new object();
        private static HashSet<string> names = Create(Defaults);

        public static bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            lock (Sync)
                return names.Contains(name);
        }

        public static int Count
        {
            get
            {
                lock (Sync)
                    return names.Count;
            }
        }

        /// <summary>Reads the file, or creates it with the defaults. An unreadable file keeps the defaults (WARN).</summary>
        public static void Configure(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(file, DefaultFile(), new UTF8Encoding(false));
                }

                var list = Parse(File.ReadAllText(file, Encoding.UTF8));
                if (list == null)
                {
                    Log(TracingLevel.WARN, $"IllegalItems: {file} ignored (expected {{ \"{ListField}\": [ \"bypass\", ... ] }}), default list used");
                    Set(Defaults);
                    return;
                }

                Set(list);
                Log(TracingLevel.INFO, $"IllegalItems: {file} ({list.Count} items)");
            }
            catch (Exception ex)
            {
                Set(Defaults);
                Log(TracingLevel.WARN, $"IllegalItems: {file} unreadable, default list used: {ex.Message}");
            }
        }

        /// <summary>{ "objets": [ ... ] } or a plain array; null if the text is not one of them.</summary>
        public static List<string> Parse(string json)
        {
            try
            {
                var token = JToken.Parse(json ?? "");
                var array = token as JArray ?? (token as JObject)?[ListField] as JArray;
                if (array == null)
                    return null;

                return array.Where(t => t.Type == JTokenType.String)
                    .Select(t => ((string)t).Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string DefaultFile()
        {
            var json = new JObject
            {
                { "#comment", "Objets du sac à dos comptés comme interdits par le plugin ZV Stream Deck Elite (calc.Backpack.Illegal). "
                    + "Noms internes du jeu : ceux des clés calc.Backpack.<nom> (bypass = Omnipasse). Les objets volés sont comptés en plus. "
                    + "Fichier relu au démarrage du plugin." },
                { ListField, new JArray(Defaults) },
            };
            return json.ToString(Formatting.Indented);
        }

        internal static void Set(IEnumerable<string> list)
        {
            var created = Create(list);
            lock (Sync)
                names = created;
        }

        internal static void Reset()
        {
            Set(Defaults);
        }

        private static HashSet<string> Create(IEnumerable<string> list)
        {
            return new HashSet<string>(list ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
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
