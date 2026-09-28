using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Odyssey backpack (issue #1, v3.3): calc.Backpack.&lt;name&gt; = number carried, e.g. calc.Backpack.amm_grenade_frag.
    /// The Backpack event gives the whole content (Items, Components, Consumables, Data), BackpackChange its changes
    /// (Added / Removed). UseConsumable is not counted: the game writes a BackpackChange for it too (journals of
    /// 2026-09-26/28). Known after the first Backpack event of the session (written when disembarking).
    /// </summary>
    public static class BackpackTracker
    {
        public const string Source = "calc.Backpack";
        private static readonly string[] Parts = { "Items", "Components", "Consumables", "Data" };

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        internal static void Reset()
        {
            lock (Sync)
                Counts.Clear();
        }

        /// <summary>True when the counts may have changed.</summary>
        public static bool Observe(string eventName, JObject evt)
        {
            if (evt == null)
                return false;

            if (string.Equals(eventName, "Backpack", StringComparison.OrdinalIgnoreCase))
            {
                if (!Parts.Any(p => evt[p] is JArray))
                    return false; // old format: the content is only in Backpack.json

                lock (Sync)
                {
                    Counts.Clear();
                    foreach (var part in Parts)
                        Add(evt[part] as JArray, 1);
                }
                return true;
            }

            if (string.Equals(eventName, "BackpackChange", StringComparison.OrdinalIgnoreCase))
            {
                lock (Sync)
                {
                    Add(evt["Added"] as JArray, 1);
                    Add(evt["Removed"] as JArray, -1);
                }
                return true;
            }

            return false;
        }

        public static Dictionary<string, JToken> Keys()
        {
            var keys = new Dictionary<string, JToken>(StoreKeys.Comparer);
            lock (Sync)
            {
                foreach (var pair in Counts)
                    keys[Source + "." + pair.Key] = new JValue(pair.Value);
            }
            return keys;
        }

        // caller holds Sync
        private static void Add(JArray items, int sign)
        {
            if (items == null)
                return;

            foreach (var item in items.OfType<JObject>())
            {
                var name = (string)item["Name"];
                if (string.IsNullOrEmpty(name))
                    continue;

                var countToken = item["Count"];
                int count = countToken != null && countToken.Type == JTokenType.Integer ? (int)countToken : 1;
                int current;
                Counts.TryGetValue(name, out current);
                Counts[name] = Math.Max(0, current + sign * count);
            }
        }
    }
}
