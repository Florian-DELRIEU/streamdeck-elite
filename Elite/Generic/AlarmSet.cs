using System;
using System.Collections.Generic;
using System.Linq;
using EliteJournalReader;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Alarms of one Alarm key (v3.6, issue #1, docs/L10-v3.md): alarm 1, plus alarms 2 to 4 when their event is set.
    /// Only the active alarm counts: the most recently triggered active alarm is shown, and a press acknowledges that
    /// one (its command is sent); at rest, a press sends the command of alarm 1 (behaviour of L8). Each alarm keeps
    /// the IsLive guard. Pure: the time is given by the caller.
    /// </summary>
    public class AlarmSet
    {
        public const int MaxAlarms = 4;

        private class Entry
        {
            public AlarmConfig Config;
            public AlarmState State = new AlarmState();
            public DateTime TriggeredAt = DateTime.MinValue;
            public long Order;
        }

        private readonly List<Entry> entries;
        private long order;

        public AlarmSet(IEnumerable<AlarmConfig> configs)
        {
            entries = configs.Select(c => new Entry { Config = c }).ToList();
            if (entries.Count == 0)
                entries.Add(new Entry { Config = AlarmConfig.FromSettings(null) });
        }

        public int Count
        {
            get { return entries.Count; }
        }

        /// <summary>Alarm 1 always; alarms 2 to 4 when their event is set.</summary>
        public static AlarmSet FromSettings(JObject settings, Action<int, string> warn = null)
        {
            var configs = new List<AlarmConfig>();
            for (int alarm = 1; alarm <= MaxAlarms; alarm++)
            {
                int number = alarm;
                var config = AlarmConfig.FromSettings(settings, warn == null ? (Action<string>)null : message => warn(number, message), alarm);
                if (alarm == 1 || config.Event.Length > 0)
                    configs.Add(config);
            }
            return new AlarmSet(configs);
        }

        /// <summary>Settings of alarm n (1 = the first); alarm 1 when n is unknown.</summary>
        public AlarmConfig Config(int index)
        {
            return entries[index >= 0 && index < entries.Count ? index : 0].Config;
        }

        /// <summary>Alerts in progress keep going when the settings change (same position in the key).</summary>
        public void KeepStatesOf(AlarmSet previous)
        {
            if (previous == null)
                return;
            for (int i = 0; i < entries.Count && i < previous.entries.Count; i++)
            {
                entries[i].State = previous.entries[i].State;
                entries[i].TriggeredAt = previous.entries[i].TriggeredAt;
                entries[i].Order = previous.entries[i].Order;
            }
            order = previous.order;
        }

        /// <summary>A journal event: indexes of the alarms it triggers (IsLive guard of each alarm).</summary>
        public List<int> Observe(RawJournalEventArgs e, DateTime now)
        {
            var triggered = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Config.ShouldTrigger(e))
                {
                    Trigger(i, now);
                    triggered.Add(i);
                }
            }
            return triggered;
        }

        /// <summary>Triggers an alarm (event received, or "Tester l'alarme").</summary>
        public void Trigger(int index, DateTime now)
        {
            if (index < 0 || index >= entries.Count)
                return;
            var entry = entries[index];
            entry.State.Trigger(now, entry.Config.DurationSeconds);
            entry.TriggeredAt = now;
            entry.Order = ++order;
        }

        /// <summary>Index of the displayed alarm: the most recently triggered of the active ones; null at rest.</summary>
        public int? Active(DateTime now)
        {
            int? best = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].State.IsActive(now) && (best == null || entries[i].Order > entries[best.Value].Order))
                    best = i;
            }
            return best;
        }

        /// <summary>Ends the displayed alarm; its index, or null at rest (nothing acknowledged).</summary>
        public int? Acknowledge(DateTime now)
        {
            var active = Active(now);
            if (active.HasValue)
                entries[active.Value].State.Acknowledge(now);
            return active;
        }
    }
}
