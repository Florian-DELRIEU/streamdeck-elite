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
    /// v3.8 (docs/L11-v3.8.md): illegal items, i.e. items of the IllegalItems list and stolen items (CollectItems with
    /// "Stolen": true, written just before its BackpackChange): calc.Backpack.Illegal / IllegalCount / Stolen.
    /// </summary>
    public static class BackpackTracker
    {
        public const string Source = "calc.Backpack";
        public const string IllegalKey = Source + ".Illegal";
        public const string IllegalCountKey = Source + ".IllegalCount";
        public const string StolenKey = Source + ".Stolen";
        private static readonly string[] Parts = { "Items", "Components", "Consumables", "Data" };

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> StolenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static bool known;

        internal static void Reset()
        {
            lock (Sync)
            {
                Counts.Clear();
                StolenCounts.Clear();
                known = false;
            }
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
                    CapStolen();
                    known = true;
                }
                return true;
            }

            if (string.Equals(eventName, "BackpackChange", StringComparison.OrdinalIgnoreCase))
            {
                lock (Sync)
                {
                    Add(evt["Added"] as JArray, 1);
                    Add(evt["Removed"] as JArray, -1);
                    if (evt["Removed"] is JArray)
                        CapStolen();
                    known = true;
                }
                return true;
            }

            if (string.Equals(eventName, "CollectItems", StringComparison.OrdinalIgnoreCase))
            {
                var name = (string)evt["Name"];
                var stolen = evt["Stolen"];
                if (string.IsNullOrEmpty(name) || stolen == null || stolen.Type != JTokenType.Boolean || !(bool)stolen)
                    return false;

                lock (Sync)
                {
                    int current;
                    StolenCounts.TryGetValue(name, out current);
                    StolenCounts[name] = current + CountOf(evt);
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

                if (known)
                {
                    int stolen = 0, illegal = 0;
                    foreach (var pair in Counts)
                    {
                        int stolenHere;
                        StolenCounts.TryGetValue(pair.Key, out stolenHere);
                        // a stolen item is written by CollectItems before its BackpackChange: counted once it is carried
                        stolenHere = Math.Min(stolenHere, pair.Value);
                        stolen += stolenHere;
                        illegal += IllegalItems.Contains(pair.Key) ? pair.Value : stolenHere;
                    }

                    keys[StolenKey] = new JValue(stolen);
                    keys[IllegalCountKey] = new JValue(illegal);
                    keys[IllegalKey] = new JValue(illegal > 0);
                }
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

                int current;
                Counts.TryGetValue(name, out current);
                Counts[name] = Math.Max(0, current + sign * CountOf(item));
            }
        }

        // caller holds Sync: a stolen item that left the backpack (dropped, sold, moved to the ship locker) is forgotten;
        // the removed unit is not known, so the stolen ones are removed last
        private static void CapStolen()
        {
            foreach (var name in StolenCounts.Keys.ToList())
            {
                int count;
                Counts.TryGetValue(name, out count);
                if (count <= 0)
                    StolenCounts.Remove(name);
                else if (StolenCounts[name] > count)
                    StolenCounts[name] = count;
            }
        }

        private static int CountOf(JObject item)
        {
            var countToken = item["Count"];
            return countToken != null && countToken.Type == JTokenType.Integer ? (int)countToken : 1;
        }
    }
}
