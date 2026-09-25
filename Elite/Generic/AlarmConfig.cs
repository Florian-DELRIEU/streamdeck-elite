using System;
using EliteJournalReader;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Settings of an Alarm key (action com.mhwlng.elite.eventalarm, docs/L8-alarme.md), read from the settings JSON:
    /// event, filterField, filterOp, filterValue, duration, idleImage, activeImage, alarmSound, and the press settings
    /// pressCommand, pressHotkey, pressHotkeyText, clickSound (same names as view 1 of a Data key).
    /// Pure: no state, no logging (warnings go to the optional callback).
    /// </summary>
    public class AlarmConfig
    {
        public const double DefaultDurationSeconds = 5;
        private const string NoFile = "No file...";

        /// <summary>Event name as written by the game (UnderAttack); compared ignoring case.</summary>
        public string Event { get; private set; } = "";

        /// <summary>Path of the tested field inside the event (Target, Health, Inventory.#count); empty = no filter.</summary>
        public string FilterField { get; private set; } = "";
        public ConditionOperator FilterOperator { get; private set; }
        public string FilterValue { get; private set; } = "";

        /// <summary>Alert duration in seconds; 0 = until a key press.</summary>
        public double DurationSeconds { get; private set; } = DefaultDurationSeconds;

        public string IdleImage { get; private set; } = "";
        public string ActiveImage { get; private set; } = "";
        public string AlarmSound { get; private set; } = "";
        public string Command { get; private set; } = "";
        public string Hotkey { get; private set; } = "";
        public string HotkeyText { get; private set; } = "";
        public string ClickSound { get; private set; } = "";

        /// <summary>A filter needs a field and a test; otherwise every event of the type triggers.</summary>
        public bool HasFilter
        {
            get { return FilterField.Length > 0 && FilterOperator != ConditionOperator.None; }
        }

        public static AlarmConfig FromSettings(JObject settings, Action<string> warn = null)
        {
            settings = settings ?? new JObject();
            var config = new AlarmConfig
            {
                Event = EventName(Text(settings, "event")),
                FilterOperator = Condition.ParseOperator(Text(settings, "filterOp")),
                FilterValue = Text(settings, "filterValue"),
                IdleImage = FileName(settings, "idleImage"),
                ActiveImage = FileName(settings, "activeImage"),
                AlarmSound = FileName(settings, "alarmSound"),
                Command = Text(settings, "pressCommand").Trim(),
                Hotkey = Text(settings, "pressHotkey").Trim(),
                HotkeyText = Text(settings, "pressHotkeyText").Trim(),
                ClickSound = FileName(settings, "clickSound"),
            };
            config.FilterField = FieldPath(Text(settings, "filterField"), config.Event);

            var duration = Text(settings, "duration").Trim();
            double seconds;
            if (duration.Length == 0)
                config.DurationSeconds = DefaultDurationSeconds;
            else if (Condition.TryParseNumber(duration, out seconds) && seconds >= 0)
                config.DurationSeconds = seconds;
            else
            {
                config.DurationSeconds = DefaultDurationSeconds;
                warn?.Invoke($"invalid duration '{duration}', {DefaultDurationSeconds} s used");
            }

            return config;
        }

        /// <summary>
        /// True for an event of the configured type that passes the filter. The field is read with the store conventions
        /// (StoreKeys.FromJournalEvent), so that the filter sees what a Data key would show. An absent field fails the
        /// filter, as in Condition.
        /// </summary>
        public bool Matches(string eventName, JObject evt)
        {
            if (Event.Length == 0 || evt == null || !string.Equals(eventName, Event, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!HasFilter)
                return true;

            JToken value;
            StoreKeys.FromJournalEvent(eventName, evt).TryGetValue(StoreKeys.JournalSource(eventName) + "." + FilterField, out value);
            return Condition.Evaluate(value, FilterOperator, FilterValue);
        }

        /// <summary>
        /// IsLive guard (cahier des charges §5.3): the events replayed at startup never trigger the alarm.
        /// </summary>
        public bool ShouldTrigger(RawJournalEventArgs e)
        {
            return e != null && e.IsLive && Matches(e.EventName, e.Event);
        }

        // "journal.UnderAttack" typed by hand is accepted as "UnderAttack"
        private static string EventName(string text)
        {
            var name = text.Trim();
            var prefix = StoreKeys.JournalPrefix + ".";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(prefix.Length);
            return name;
        }

        // a full key "journal.UnderAttack.Target" is accepted as "Target"
        private static string FieldPath(string text, string eventName)
        {
            var path = text.Trim();
            var prefix = StoreKeys.JournalSource(eventName) + ".";
            if (eventName.Length > 0 && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                path = path.Substring(prefix.Length);
            return path;
        }

        private static string Text(JObject settings, string name)
        {
            var token = settings[name];
            if (token == null || token.Type == JTokenType.Null)
                return "";
            return token.Type == JTokenType.String ? (string)token : token.ToString();
        }

        // the property inspector may send "No file..." for an empty file field
        private static string FileName(JObject settings, string name)
        {
            var value = Text(settings, name).Trim();
            return value == NoFile ? "" : value;
        }
    }
}
