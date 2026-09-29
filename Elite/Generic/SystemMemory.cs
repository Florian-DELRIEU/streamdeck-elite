using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Visited systems (v3.8, docs/L11-v3.8.md): the summary of each system (SystemTracker.Summarize) at the last visit,
    /// by SystemAddress, saved in the "systems" section of PluginMemory. The game says nothing of a target system before
    /// the arrival (population, factions...): target.* uses the last visit. The last MaxSystems systems are kept.
    /// </summary>
    public static class SystemMemory
    {
        public const int MaxSystems = 1000;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, JObject> Systems = new Dictionary<string, JObject>(StringComparer.Ordinal);

        public static int Count
        {
            get
            {
                lock (Sync)
                    return Systems.Count;
            }
        }

        internal static void Load(JObject systems)
        {
            lock (Sync)
            {
                Systems.Clear();
                if (systems == null)
                    return;

                foreach (var property in systems.Properties())
                {
                    if (property.Value is JObject entry)
                        Systems[property.Name] = entry;
                }
                Prune();
            }
        }

        internal static void Reset()
        {
            Load(null);
        }

        internal static JObject Snapshot()
        {
            lock (Sync)
            {
                var json = new JObject();
                foreach (var pair in Systems.OrderBy(p => Visited(p.Value)))
                    json[pair.Key] = pair.Value.DeepClone();
                return json;
            }
        }

        /// <summary>Copy of the last visit of a system, null if never seen.</summary>
        public static JObject Find(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            lock (Sync)
            {
                JObject entry;
                return Systems.TryGetValue(address, out entry) ? (JObject)entry.DeepClone() : null;
            }
        }

        /// <summary>Remembers a summary (key: its Address), then saves the file (at most every 30 s).</summary>
        public static void Remember(JObject summary)
        {
            var address = summary?["Address"]?.ToString();
            if (string.IsNullOrEmpty(address))
                return;

            lock (Sync)
            {
                Systems[address] = (JObject)summary.DeepClone();
                Prune();
            }

            PluginMemory.RequestSave();
        }

        // caller holds Sync: the oldest visits go first
        private static void Prune()
        {
            if (Systems.Count <= MaxSystems)
                return;

            foreach (var old in Systems.OrderBy(p => Visited(p.Value)).Take(Systems.Count - MaxSystems).Select(p => p.Key).ToList())
                Systems.Remove(old);
        }

        private static DateTime Visited(JObject entry)
        {
            return DerivedKeys.Time(entry[SystemTracker.VisitedField]) ?? DateTime.MinValue;
        }
    }
}
