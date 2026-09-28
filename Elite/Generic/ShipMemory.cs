using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Characteristic values of each ship, remembered from its last Loadout (live or replayed at startup) and saved in
    /// PluginMemory (issue #2, v3.3). The current ship (last LoadGame, ShipyardSwap or Loadout) gives the ship.* keys:
    /// ship.ShipID, ship.Ship, ship.ShipName, ship.ShipIdent, ship.CargoCapacity, ship.FuelCapacity.Main/Reserve,
    /// ship.MaxJumpRange, ship.HullHealth, ship.UnladenMass, ship.FsdOvercharge, ship.Fsd.*, ship.GuardianBoost.
    /// </summary>
    public static class ShipMemory
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, JObject> Ships = new Dictionary<string, JObject>(StringComparer.Ordinal);
        private static string current;

        public static string CurrentShipId
        {
            get
            {
                lock (Sync)
                    return current;
            }
        }

        public static int ShipCount
        {
            get
            {
                lock (Sync)
                    return Ships.Count;
            }
        }

        internal static void Load(JObject ships, string currentShip)
        {
            lock (Sync)
            {
                Ships.Clear();
                if (ships != null)
                {
                    foreach (var property in ships.Properties())
                    {
                        if (property.Value is JObject entry)
                            Ships[property.Name] = entry;
                    }
                }

                current = string.IsNullOrEmpty(currentShip) ? null : currentShip;
            }
        }

        internal static void Reset()
        {
            Load(null, null);
        }

        internal static JObject Snapshot()
        {
            lock (Sync)
            {
                var json = new JObject();
                foreach (var pair in Ships.OrderBy(p => p.Key, StringComparer.Ordinal))
                    json[pair.Key] = pair.Value.DeepClone();
                return json;
            }
        }

        /// <summary>
        /// Journal event (live or replayed): Loadout remembers the ship; LoadGame, ShipyardSwap and Loadout select the
        /// current ship. True when the ship.* keys may have changed.
        /// </summary>
        public static bool Observe(string eventName, JObject evt)
        {
            if (evt == null)
                return false;

            bool loadout = string.Equals(eventName, "Loadout", StringComparison.OrdinalIgnoreCase);
            if (!loadout && !string.Equals(eventName, "LoadGame", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(eventName, "ShipyardSwap", StringComparison.OrdinalIgnoreCase))
                return false;

            var idToken = evt["ShipID"];
            if (idToken == null || idToken.Type == JTokenType.Null)
                return false;
            var id = idToken.ToString();

            lock (Sync)
            {
                if (loadout)
                    Ships[id] = FromLoadout(evt);
                else if (!Ships.ContainsKey(id))
                {
                    // a ship never seen in a Loadout: its identity only
                    var ship = evt["Ship"] ?? evt["ShipType"];
                    Ships[id] = new JObject { { "ShipID", idToken.DeepClone() }, { "Ship", ship == null ? null : ship.DeepClone() } };
                }

                current = id;
            }

            PluginMemory.RequestSave();
            return true;
        }

        /// <summary>ship.* keys of the current ship (none before the first LoadGame / ShipyardSwap / Loadout).</summary>
        public static Dictionary<string, JToken> Keys()
        {
            var keys = new Dictionary<string, JToken>(StoreKeys.Comparer);
            lock (Sync)
            {
                JObject entry;
                if (current == null || !Ships.TryGetValue(current, out entry))
                    return keys;
                Flatten(entry, StoreKeys.ShipPrefix, keys);
            }

            return keys;
        }

        /// <summary>What is remembered of a ship, from its Loadout event (pure function).</summary>
        public static JObject FromLoadout(JObject loadout)
        {
            var entry = new JObject();
            foreach (var name in new[] { "ShipID", "Ship", "ShipName", "ShipIdent", "CargoCapacity", "MaxJumpRange", "HullHealth", "UnladenMass" })
            {
                var value = loadout[name];
                if (value != null && value.Type != JTokenType.Null)
                    entry[name] = value.DeepClone();
            }

            if (loadout["FuelCapacity"] is JObject fuel)
                entry["FuelCapacity"] = fuel.DeepClone();

            var modules = (loadout["Modules"] as JArray ?? new JArray()).OfType<JObject>().ToList();
            var fsdModule = modules.FirstOrDefault(m => Item(m).Contains("hyperdrive"));
            entry["FsdOvercharge"] = fsdModule != null && Item(fsdModule).Contains("_overcharge_");

            FsdData table;
            if (fsdModule != null && FsdTables.Drives.TryGetValue(Item(fsdModule), out table))
            {
                // engineering changes the optimal mass and the maximum fuel per jump
                entry["Fsd"] = new JObject
                {
                    { "Module", Item(fsdModule) },
                    { "OptimalMass", Modified(fsdModule, "FSDOptimalMass", table.OptimalMass) },
                    { "MaxFuelPerJump", Modified(fsdModule, "MaxFuelPerJump", table.MaxFuelPerJump) },
                    { "FuelMultiplier", table.FuelMultiplier },
                    { "FuelPower", table.FuelPower },
                };
            }

            double boost = 0;
            foreach (var module in modules)
            {
                double value;
                if (FsdTables.GuardianBoosters.TryGetValue(Item(module), out value))
                    boost += value;
            }
            entry["GuardianBoost"] = boost;
            return entry;
        }

        private static string Item(JObject module)
        {
            return ((string)module["Item"] ?? "").ToLowerInvariant();
        }

        private static double Modified(JObject module, string label, double original)
        {
            var modifiers = module["Engineering"]?["Modifiers"] as JArray;
            var modifier = modifiers?.OfType<JObject>().FirstOrDefault(m => string.Equals((string)m["Label"], label, StringComparison.OrdinalIgnoreCase));
            var value = modifier?["Value"];
            return value != null && (value.Type == JTokenType.Float || value.Type == JTokenType.Integer) ? (double)value : original;
        }

        private static void Flatten(JObject json, string prefix, Dictionary<string, JToken> keys)
        {
            foreach (var property in json.Properties())
            {
                var key = prefix + "." + property.Name;
                if (property.Value is JObject child)
                    Flatten(child, key, keys);
                else if (property.Value.Type != JTokenType.Null && property.Value.Type != JTokenType.Array)
                    keys[key] = property.Value.DeepClone();
            }
        }
    }
}
