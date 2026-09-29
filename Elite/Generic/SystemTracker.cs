using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Current system (system.*) and target system (target.*), v3.8 (issue #1, docs/L11-v3.8.md). Stateful: the store
    /// keeps only the flat keys of the last event of each type, where the lists (Factions, Conflicts) are reduced to their
    /// count; this class reads the raw events.
    /// - arrival (FSDJump, Location, CarrierJump) -> summary of the system (Summarize), remembered in SystemMemory;
    /// - main star: StartJump (hyperspace, class of the next system), then the Scan of the arrival star, else the memory;
    /// - target (FSDTarget) until the arrival in it or NavRouteClear (same rule as calc.Route.RemainingJumps), with the
    ///   last visit of the target system when it is known.
    /// </summary>
    public static class SystemTracker
    {
        public const string VisitedField = "Visited";

        // star classes whose star can be scooped ("KGB FOAM"), giants included (K_OrangeGiant, M_RedSuperGiant...)
        private static readonly HashSet<string> ScoopableClasses = new HashSet<string>(StringComparer.Ordinal) { "O", "B", "A", "F", "G", "K", "M" };

        private static readonly Dictionary<string, string> States = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "None", "Aucun" }, { "Boom", "Boom" }, { "Bust", "Récession" }, { "CivilUnrest", "Troubles civils" },
            { "CivilWar", "Guerre civile" }, { "Election", "Élections" }, { "Expansion", "Expansion" }, { "Famine", "Famine" },
            { "Investment", "Investissement" }, { "Lockdown", "Confinement" }, { "Outbreak", "Épidémie" },
            { "Retreat", "Retraite" }, { "War", "Guerre" }, { "CivilLiberty", "Libertés civiles" },
            { "PirateAttack", "Attaque pirate" }, { "Terrorism", "Attaque terroriste" }, { "NaturalDisaster", "Catastrophe naturelle" },
            { "InfrastructureFailure", "Panne d'infrastructure" }, { "Drought", "Sécheresse" }, { "Blight", "Fléau" },
            { "PublicHoliday", "Jour férié" }, { "ColdWar", "Guerre froide" }, { "TradeWar", "Guerre commerciale" },
        };

        private static readonly Dictionary<string, string> WarTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "war", "Guerre" }, { "civilwar", "Guerre civile" }, { "election", "Élections" },
        };

        private static readonly Dictionary<string, string> WarStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "active", "En cours" }, { "pending", "En attente" }, { "", "Terminé" },
        };

        private static readonly object Sync = new object();
        private static JObject current;
        private static string pendingStarAddress, pendingStarClass;
        private static JObject target;
        private static DateTime? targetTime, clearTime;

        internal static void Reset()
        {
            lock (Sync)
            {
                current = null;
                pendingStarAddress = pendingStarClass = null;
                target = null;
                targetTime = clearTime = null;
            }
        }

        /// <summary>True when the system.* or target.* keys may have changed.</summary>
        public static bool Observe(string eventName, JObject evt)
        {
            if (evt == null || string.IsNullOrEmpty(eventName))
                return false;

            switch (eventName.ToLowerInvariant())
            {
                case "fsdjump":
                case "location":
                case "carrierjump":
                    return Arrive(evt);
                case "startjump":
                    if (!string.Equals((string)evt["JumpType"], "Hyperspace", StringComparison.OrdinalIgnoreCase) || evt["StarClass"] == null)
                        return false;
                    lock (Sync)
                    {
                        pendingStarAddress = Text(evt["SystemAddress"]);
                        pendingStarClass = Text(evt["StarClass"]);
                    }
                    return false;
                case "scan":
                    return ScanStar(evt);
                case "fsdtarget":
                    lock (Sync)
                    {
                        target = (JObject)evt.DeepClone();
                        targetTime = DerivedKeys.Time(evt["timestamp"]);
                    }
                    return true;
                case "navrouteclear":
                    lock (Sync)
                        clearTime = DerivedKeys.Time(evt["timestamp"]);
                    return true;
                default:
                    return false;
            }
        }

        public static Dictionary<string, JToken> SystemKeys()
        {
            var keys = new Dictionary<string, JToken>(StoreKeys.Comparer);
            lock (Sync)
            {
                if (current != null)
                    Flatten(current, StoreKeys.SystemPrefix, keys, VisitedField);
            }
            return keys;
        }

        public static Dictionary<string, JToken> TargetKeys()
        {
            var keys = new Dictionary<string, JToken>(StoreKeys.Comparer);
            JObject selected;
            lock (Sync)
            {
                if (target == null || targetTime == null)
                    return keys;
                var name = Text(target["Name"]);
                if (current != null && name.Length > 0 && string.Equals(name, Text(current["Name"]), StringComparison.OrdinalIgnoreCase))
                    return keys; // arrived
                if (clearTime != null && clearTime.Value >= targetTime.Value)
                    return keys; // route cleared
                selected = target;
            }

            var address = Text(selected["SystemAddress"]);
            var visit = SystemMemory.Find(address);
            string prefix = StoreKeys.TargetPrefix;
            if (visit != null)
            {
                Flatten(visit, prefix, keys, VisitedField);
                var visited = visit[VisitedField];
                if (visited != null)
                    keys[prefix + ".LastVisit"] = visited.DeepClone();
            }

            keys[prefix + ".Known"] = new JValue(visit != null);
            Put(keys, prefix + ".Name", selected["Name"]);
            Put(keys, prefix + ".Address", selected["SystemAddress"]);
            var starClass = Text(selected["StarClass"]);
            if (starClass.Length > 0)
            {
                keys[prefix + ".Star.Class"] = new JValue(starClass);
                keys[prefix + ".Star.Scoopable"] = new JValue(IsScoopable(starClass));
            }
            Put(keys, prefix + ".RemainingJumps", selected["RemainingJumpsInRoute"]);
            return keys;
        }

        /// <summary>True for the classes O, B, A, F, G, K, M, giants included ("KGB FOAM"); false otherwise.</summary>
        public static bool IsScoopable(string starClass)
        {
            if (string.IsNullOrEmpty(starClass))
                return false;
            var main = starClass.Split('_')[0];
            return ScoopableClasses.Contains(main);
        }

        /// <summary>Hostile, Inamical, Neutre, Cordial, Ami, Allié from MyReputation (-100 to +100).</summary>
        public static string Relation(double reputation)
        {
            if (reputation <= -90)
                return "Hostile";
            if (reputation <= -35)
                return "Inamical";
            if (reputation < 4)
                return "Neutre";
            if (reputation < 35)
                return "Cordial";
            if (reputation < 90)
                return "Ami";
            return "Allié";
        }

        /// <summary>What is kept of a system from its arrival event (FSDJump, Location, CarrierJump): pure function.</summary>
        public static JObject Summarize(JObject evt)
        {
            var summary = new JObject();
            Put(summary, "Name", evt["StarSystem"]);
            Put(summary, "Address", evt["SystemAddress"]);
            Put(summary, "Population", evt["Population"]);
            Put(summary, "Allegiance", evt["SystemAllegiance"]);
            PutText(summary, "Government", Localised(evt, "SystemGovernment"));
            PutText(summary, "Economy", Localised(evt, "SystemEconomy"));
            PutText(summary, "SecondEconomy", Localised(evt, "SystemSecondEconomy"));
            PutText(summary, "Security", Localised(evt, "SystemSecurity"));
            Put(summary, "Power", evt["ControllingPower"]);
            Put(summary, "PowerplayState", evt["PowerplayState"]);
            var wanted = evt["Wanted"];
            summary["Wanted"] = wanted != null && wanted.Type == JTokenType.Boolean && (bool)wanted;

            var factions = (evt["Factions"] as JArray ?? new JArray()).OfType<JObject>().ToList();
            var controlling = Text(evt["SystemFaction"]?["Name"]);
            var faction = factions.FirstOrDefault(f => string.Equals(Text(f["Name"]), controlling, StringComparison.OrdinalIgnoreCase));
            if (controlling.Length > 0)
            {
                var entry = new JObject { { "Name", controlling } };
                var state = Text(faction?["FactionState"] ?? evt["SystemFaction"]?["FactionState"]);
                if (state.Length > 0)
                    entry["State"] = Translate(States, state);
                if (faction != null)
                {
                    var influence = Number(faction["Influence"]);
                    if (influence.HasValue)
                        entry["Influence"] = Math.Round(influence.Value * 100, 1, MidpointRounding.AwayFromZero);
                    PutText(entry, "Happiness", Localised(faction, "Happiness"));
                    var reputation = Number(faction["MyReputation"]);
                    if (reputation.HasValue)
                    {
                        entry["Reputation"] = Math.Round(reputation.Value, 1, MidpointRounding.AwayFromZero);
                        entry["Relation"] = Relation(reputation.Value);
                    }
                    Put(entry, "Government", faction["Government"]);
                    Put(entry, "Allegiance", faction["Allegiance"]);
                }
                summary["Faction"] = entry;
            }

            var conflicts = (evt["Conflicts"] as JArray ?? new JArray()).OfType<JObject>().ToList();
            summary["Conflicts"] = conflicts.Count;
            var conflict = conflicts.FirstOrDefault(c => Involves(c, controlling)) ?? conflicts.FirstOrDefault();
            if (conflict != null)
            {
                // the side of the controlling faction when it is at war, otherwise Faction1
                bool second = string.Equals(Text(conflict["Faction2"]?["Name"]), controlling, StringComparison.OrdinalIgnoreCase);
                var side = conflict[second ? "Faction2" : "Faction1"];
                var other = conflict[second ? "Faction1" : "Faction2"];
                var entry = new JObject();
                PutText(entry, "Type", Translate(WarTypes, Text(conflict["WarType"])));
                entry["Status"] = Translate(WarStatus, Text(conflict["Status"]));
                Put(entry, "Faction", side?["Name"]);
                Put(entry, "Opponent", other?["Name"]);
                Put(entry, "WonDays", side?["WonDays"]);
                Put(entry, "OpponentWonDays", other?["WonDays"]);
                summary["Conflict"] = entry;
            }

            var time = DerivedKeys.Time(evt["timestamp"]);
            if (time.HasValue)
                summary[VisitedField] = time.Value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            return summary;
        }

        private static bool Arrive(JObject evt)
        {
            var summary = Summarize(evt);
            var address = Text(summary["Address"]);
            var previous = SystemMemory.Find(address);

            lock (Sync)
            {
                string starClass = null;
                if (address.Length > 0 && address == pendingStarAddress)
                    starClass = pendingStarClass;
                else if (previous?["Star"]?["Class"] != null)
                    starClass = Text(previous["Star"]["Class"]);
                else if (current != null && address == Text(current["Address"]) && current["Star"]?["Class"] != null)
                    starClass = Text(current["Star"]["Class"]);

                if (!string.IsNullOrEmpty(starClass))
                    summary["Star"] = Star(starClass);
                current = summary;
                pendingStarAddress = pendingStarClass = null;
            }

            SystemMemory.Remember(summary);
            return true;
        }

        // Scan of the arrival star (distance 0) of the current system: its class
        private static bool ScanStar(JObject evt)
        {
            var starType = Text(evt["StarType"]);
            var distance = Number(evt["DistanceFromArrivalLS"]);
            if (starType.Length == 0 || !distance.HasValue || distance.Value != 0)
                return false;

            JObject updated;
            lock (Sync)
            {
                if (current == null || Text(evt["SystemAddress"]) != Text(current["Address"]))
                    return false;
                if (string.Equals(Text(current["Star"]?["Class"]), starType, StringComparison.Ordinal))
                    return false;
                current["Star"] = Star(starType);
                updated = (JObject)current.DeepClone();
            }

            SystemMemory.Remember(updated);
            return true;
        }

        private static JObject Star(string starClass)
        {
            return new JObject { { "Class", starClass }, { "Scoopable", IsScoopable(starClass) } };
        }

        private static bool Involves(JObject conflict, string faction)
        {
            return faction.Length > 0
                && (string.Equals(Text(conflict["Faction1"]?["Name"]), faction, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Text(conflict["Faction2"]?["Name"]), faction, StringComparison.OrdinalIgnoreCase));
        }

        // "SystemGovernment_Localised" when the game writes it, otherwise the raw value without "$...;"
        private static string Localised(JObject evt, string name)
        {
            var localised = Text(evt[name + "_Localised"]);
            if (localised.Length > 0)
                return localised;
            var raw = Text(evt[name]);
            if (raw.StartsWith("$", StringComparison.Ordinal) && raw.EndsWith(";", StringComparison.Ordinal))
            {
                raw = raw.Substring(1, raw.Length - 2);
                int underscore = raw.IndexOf('_');
                if (underscore >= 0)
                    raw = raw.Substring(underscore + 1);
            }
            return raw;
        }

        private static string Translate(Dictionary<string, string> table, string value)
        {
            string translated;
            return table.TryGetValue(value ?? "", out translated) ? translated : value;
        }

        private static double? Number(JToken token)
        {
            return token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer) ? (double)token : (double?)null;
        }

        private static string Text(JToken token)
        {
            return DerivedKeys.Text(token);
        }

        private static void Put(JObject json, string name, JToken value)
        {
            if (value != null && value.Type != JTokenType.Null)
                json[name] = value.DeepClone();
        }

        private static void Put(Dictionary<string, JToken> keys, string key, JToken value)
        {
            if (value != null && value.Type != JTokenType.Null)
                keys[key] = value.DeepClone();
        }

        private static void PutText(JObject json, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
                json[name] = value;
        }

        private static void Flatten(JObject json, string prefix, Dictionary<string, JToken> keys, string skipped)
        {
            foreach (var property in json.Properties())
            {
                if (string.Equals(property.Name, skipped, StringComparison.Ordinal))
                    continue;
                var key = prefix + "." + property.Name;
                if (property.Value is JObject child)
                    Flatten(child, key, keys, null);
                else if (property.Value.Type != JTokenType.Null && property.Value.Type != JTokenType.Array)
                    keys[key] = property.Value.DeepClone();
            }
        }
    }
}
