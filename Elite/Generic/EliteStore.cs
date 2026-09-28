using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BarRaider.SdTools;
using EliteJournalReader;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("Elite.Tests")]

namespace Elite.Generic
{
    /// <summary>
    /// Last known value of every game data, as flat keys: status.* (status.json) and journal.* (last event of each type).
    /// Thread-safe: the watchers call it from background threads. Keys are case-insensitive.
    /// </summary>
    public static class EliteStore
    {
        private static readonly object Sync = new object();

        private static readonly Dictionary<string, JToken> Values = new Dictionary<string, JToken>(StoreKeys.Comparer);

        // keys currently held by each source ("status" or "journal.<Event>"), to detect the keys that disappeared
        private static readonly Dictionary<string, HashSet<string>> KeysBySource = new Dictionary<string, HashSet<string>>(StoreKeys.Comparer);

        // reserved for the future Counter action: live occurrences per event type (not exposed in V1)
        private static readonly Dictionary<string, int> LiveCounts = new Dictionary<string, int>(StoreKeys.Comparer);

        /// <summary>
        /// Keys whose value changed, appeared or disappeared. Contains() on this collection is case-insensitive.
        /// Raised from background threads, outside of the store lock.
        /// </summary>
        public static event Action<IReadOnlyCollection<string>> DataChanged;

        /// <summary>
        /// Every journal event, after the store was updated. IsLive is false for events replayed at startup.
        /// </summary>
        public static event EventHandler<RawJournalEventArgs> JournalEventReceived;

        public static int KeyCount
        {
            get
            {
                lock (Sync)
                    return Values.Count;
            }
        }

        public static bool TryGet(string key, out JToken value)
        {
            value = null;
            if (string.IsNullOrEmpty(key))
                return false;

            lock (Sync)
            {
                JToken stored;
                if (!Values.TryGetValue(key, out stored))
                    return false;

                value = stored.DeepClone();
                return true;
            }
        }

        public static void HandleRawStatus(object sender, RawStatusEventArgs e)
        {
            try
            {
                if (e == null || e.Status == null)
                    return;

                UpdateStatus(e.Status);
            }
            catch (Exception ex)
            {
                Log(TracingLevel.ERROR, $"EliteStore: status update failed: {ex}");
            }
        }

        public static void HandleRawJournal(object sender, RawJournalEventArgs e)
        {
            try
            {
                if (e == null || string.IsNullOrEmpty(e.EventName) || e.Event == null)
                    return;

                UpdateJournal(e);
            }
            catch (Exception ex)
            {
                Log(TracingLevel.ERROR, $"EliteStore: journal update failed for {e?.EventName}: {ex}");
            }
        }

        internal static void UpdateStatus(JObject status)
        {
            var changed = Replace(StoreKeys.StatusPrefix, StoreKeys.FromStatus(status));
            changed.UnionWith(UpdateDerived());
            Raise(changed);
        }

        internal static void UpdateJournal(RawJournalEventArgs e)
        {
            if (e.IsLive)
            {
                lock (Sync)
                {
                    int count;
                    LiveCounts.TryGetValue(e.EventName, out count);
                    LiveCounts[e.EventName] = count + 1;
                }
            }

            var changed = Replace(StoreKeys.JournalSource(e.EventName), StoreKeys.FromJournalEvent(e.EventName, e.Event));
            changed.UnionWith(UpdateDerived());
            Raise(changed);

            var handler = JournalEventReceived;
            if (handler == null)
                return;

            foreach (EventHandler<RawJournalEventArgs> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(null, e);
                }
                catch (Exception ex)
                {
                    Log(TracingLevel.WARN, $"EliteStore: JournalEventReceived subscriber failed for {e.EventName}: {ex}");
                }
            }
        }

        internal static int GetLiveCount(string eventName)
        {
            lock (Sync)
            {
                int count;
                return LiveCounts.TryGetValue(eventName, out count) ? count : 0;
            }
        }

        /// <summary>
        /// For unit tests: empty store, no subscribers.
        /// </summary>
        internal static void Reset()
        {
            lock (Sync)
            {
                Values.Clear();
                KeysBySource.Clear();
                LiveCounts.Clear();
            }

            DataChanged = null;
            JournalEventReceived = null;
        }

        /// <summary>
        /// calc.* keys (DerivedKeys), recomputed after every status or journal update. Their changes are notified in the
        /// same DataChanged as the update that caused them (one redraw).
        /// </summary>
        private static HashSet<string> UpdateDerived()
        {
            Dictionary<string, JToken> derived;
            lock (Sync)
                derived = DerivedKeys.Compute(ReadLocked);
            return Replace(StoreKeys.CalcPrefix, derived);
        }

        // caller holds Sync
        private static JToken ReadLocked(string key)
        {
            JToken value;
            return Values.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>
        /// Replaces all the keys of one source: new or modified keys are stored, keys missing from newValues are removed.
        /// Returns the keys that changed (DataChanged is raised by the caller, with Raise).
        /// </summary>
        private static HashSet<string> Replace(string source, Dictionary<string, JToken> newValues)
        {
            var changed = new HashSet<string>(StoreKeys.Comparer);

            lock (Sync)
            {
                HashSet<string> oldKeys;
                if (KeysBySource.TryGetValue(source, out oldKeys))
                {
                    foreach (var key in oldKeys)
                    {
                        if (!newValues.ContainsKey(key))
                        {
                            Values.Remove(key);
                            changed.Add(key);
                        }
                    }
                }

                foreach (var pair in newValues)
                {
                    JToken old;
                    if (!Values.TryGetValue(pair.Key, out old) || !JToken.DeepEquals(old, pair.Value))
                    {
                        Values[pair.Key] = pair.Value;
                        changed.Add(pair.Key);
                    }
                }

                KeysBySource[source] = new HashSet<string>(newValues.Keys, StoreKeys.Comparer);
            }

            return changed;
        }

        private static void Raise(HashSet<string> changed)
        {
            if (changed.Count > 0)
                RaiseDataChanged(changed);
        }

        private static void RaiseDataChanged(IReadOnlyCollection<string> changedKeys)
        {
            var handler = DataChanged;
            if (handler == null)
                return;

            foreach (Action<IReadOnlyCollection<string>> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(changedKeys);
                }
                catch (Exception ex)
                {
                    Log(TracingLevel.WARN, $"EliteStore: DataChanged subscriber failed: {ex}");
                }
            }
        }

        private static void Log(TracingLevel level, string message)
        {
            try
            {
                Logger.Instance.LogMessage(level, message);
            }
            catch
            {
                // logging must never take the plugin down
            }
        }
    }
}
