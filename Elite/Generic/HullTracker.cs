using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// calc.Hull.Percent (v3.3): hull of the ship in %, from the last HullDamage of the ship (a damage of the fighter is
    /// ignored, even though the store keeps only the last HullDamage event), Loadout.HullHealth, and 100 after RepairAll
    /// or a Repair of the hull. The game gives no value for the shields.
    /// </summary>
    public static class HullTracker
    {
        public const string Source = "calc.Hull";
        public const string Key = "calc.Hull.Percent";

        private static readonly object Sync = new object();
        private static double? health;

        internal static void Reset()
        {
            lock (Sync)
                health = null;
        }

        /// <summary>True when the hull value may have changed.</summary>
        public static bool Observe(string eventName, JObject evt)
        {
            var value = HealthAfter(eventName, evt);
            if (!value.HasValue)
                return false;

            lock (Sync)
                health = Math.Max(0, Math.Min(1, value.Value));
            return true;
        }

        /// <summary>Hull (0 to 1) given by an event, or null when the event says nothing about the ship hull.</summary>
        public static double? HealthAfter(string eventName, JObject evt)
        {
            if (evt == null || eventName == null)
                return null;

            switch (eventName.ToLowerInvariant())
            {
                case "hulldamage":
                    var fighter = evt["Fighter"];
                    if (fighter != null && fighter.Type == JTokenType.Boolean && (bool)fighter)
                        return null;
                    return History.Number(evt["Health"]);
                case "loadout":
                    return History.Number(evt["HullHealth"]);
                case "repairall":
                    return 1;
                case "repair":
                    // older format: "Item": "hull" / "all"; newer: "Items": [ ... ]
                    var items = new List<string> { (string)evt["Item"] ?? "" };
                    if (evt["Items"] is JArray array)
                        items.AddRange(array.Select(i => i.Type == JTokenType.String ? (string)i : ""));
                    return items.Any(i => i.ToLowerInvariant().Contains("hull") || string.Equals(i, "all", StringComparison.OrdinalIgnoreCase))
                        ? 1 : (double?)null;
                default:
                    return null;
            }
        }

        public static Dictionary<string, JToken> Keys()
        {
            var keys = new Dictionary<string, JToken>(StoreKeys.Comparer);
            lock (Sync)
            {
                if (health.HasValue)
                    keys[Key] = new JValue(Math.Round(health.Value * 100, 1, MidpointRounding.AwayFromZero));
            }
            return keys;
        }
    }
}
