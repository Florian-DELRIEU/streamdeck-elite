using System;
using System.Collections.Generic;
using EliteJournalReader.Events;
using Newtonsoft.Json.Linq;

namespace Elite.Generic
{
    /// <summary>
    /// Turns the raw JSON of status.json and of journal events into flat keys
    /// (conventions: docs/cahier-des-charges §4.3 and docs/L1-socle-donnees.md).
    /// Pure functions: no state, no logging.
    /// </summary>
    public static class StoreKeys
    {
        public const string StatusPrefix = "status";
        public const string JournalPrefix = "journal";
        public const string CountSuffix = "#count";

        public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

        private static readonly string[] PipNames = { "System", "Engine", "Weapons" };

        public static string JournalSource(string eventName)
        {
            return JournalPrefix + "." + eventName;
        }

        /// <summary>
        /// status.Flags.X / status.Flags2.X (booleans), status.Pips.System/Engine/Weapons,
        /// status.GuiFocus (enum name), everything else flattened. A field absent from the file gives no key.
        /// </summary>
        public static Dictionary<string, JToken> FromStatus(JObject status)
        {
            var result = new Dictionary<string, JToken>(Comparer);
            if (status == null)
                return result;

            foreach (var property in status.Properties())
            {
                var key = StatusPrefix + "." + property.Name;
                switch (property.Name)
                {
                    case "event":
                        break;
                    case "Flags":
                        AddFlags(result, key, property.Value, typeof(StatusFlags));
                        break;
                    case "Flags2":
                        AddFlags(result, key, property.Value, typeof(MoreStatusFlags));
                        break;
                    case "Pips":
                        AddPips(result, key, property.Value);
                        break;
                    case "GuiFocus":
                        AddGuiFocus(result, key, property.Value);
                        break;
                    default:
                        Flatten(result, key, property.Value);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// journal.EventName.path, with the event name exactly as written by the game.
        /// </summary>
        public static Dictionary<string, JToken> FromJournalEvent(string eventName, JObject evt)
        {
            var result = new Dictionary<string, JToken>(Comparer);
            if (string.IsNullOrEmpty(eventName) || evt == null)
                return result;

            var source = JournalSource(eventName);
            foreach (var property in evt.Properties())
            {
                if (property.Name == "event")
                    continue;

                Flatten(result, source + "." + property.Name, property.Value);
            }

            return result;
        }

        private static void AddFlags(Dictionary<string, JToken> result, string key, JToken value, Type enumType)
        {
            if (value.Type != JTokenType.Integer)
            {
                Flatten(result, key, value);
                return;
            }

            long raw = value.Value<long>();
            foreach (var flag in Enum.GetValues(enumType))
            {
                long bits = Convert.ToInt64(flag);
                if (bits == 0)
                    continue; // None: HasFlag(None) is always true

                result[key + "." + Enum.GetName(enumType, flag)] = new JValue((raw & bits) == bits);
            }
        }

        private static void AddPips(Dictionary<string, JToken> result, string key, JToken value)
        {
            var array = value as JArray;
            if (array == null || array.Count != PipNames.Length)
            {
                Flatten(result, key, value);
                return;
            }

            for (int i = 0; i < PipNames.Length; i++)
                AddValue(result, key + "." + PipNames[i], array[i]);
        }

        private static void AddGuiFocus(Dictionary<string, JToken> result, string key, JToken value)
        {
            if (value.Type == JTokenType.Integer)
            {
                long focus = value.Value<long>();
                if (focus >= 0 && focus <= int.MaxValue && Enum.IsDefined(typeof(StatusGuiFocus), (int)focus))
                {
                    result[key] = new JValue(((StatusGuiFocus)(int)focus).ToString());
                    return;
                }
            }

            Flatten(result, key, value);
        }

        private static void Flatten(Dictionary<string, JToken> result, string key, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    foreach (var property in ((JObject)value).Properties())
                        Flatten(result, key + "." + property.Name, property.Value);
                    break;
                case JTokenType.Array:
                    result[key + "." + CountSuffix] = new JValue(((JArray)value).Count);
                    break;
                default:
                    AddValue(result, key, value);
                    break;
            }
        }

        private static void AddValue(Dictionary<string, JToken> result, string key, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                return;

            result[key] = value.DeepClone();
        }
    }
}
