using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elite.CatalogGen
{
    /// <summary>
    /// Journal keys seen in real journal files but unknown to EliteJournalReader (the library lags behind the game).
    /// "scan" writes them to observed-keys.txt (key names and types only, never values); "generate" merges them.
    /// File format: "event&lt;TAB&gt;Name" for an event name the library does not know with this exact spelling,
    /// "journal.Event.path&lt;TAB&gt;type" for a key missing from the reflection catalog. Lines starting with # are comments.
    /// </summary>
    internal static class ObservedKeys
    {
        public const string FileName = "observed-keys.txt";

        public static List<string> Scan(string journalDir, Catalog reflectionCatalog, out int fileCount, out int lineCount, out int rejected)
        {
            var knownKeys = new HashSet<string>(reflectionCatalog.AllKeys(), StringComparer.OrdinalIgnoreCase);
            var libraryEvents = new HashSet<string>(
                reflectionCatalog.Groups.Where(g => g.Prefix.StartsWith("journal.", StringComparison.Ordinal)).Select(g => g.Prefix.Substring("journal.".Length)),
                StringComparer.Ordinal);

            var eventNames = new SortedSet<string>(StringComparer.Ordinal);
            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var secrets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            fileCount = 0;
            lineCount = 0;

            foreach (var file in Directory.GetFiles(journalDir, "Journal.*.log").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                fileCount++;
                foreach (var line in File.ReadLines(file))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    JObject evt;
                    try
                    {
                        evt = JObject.Parse(line);
                    }
                    catch
                    {
                        continue;
                    }

                    var eventName = evt.Value<string>("event");
                    if (string.IsNullOrEmpty(eventName) || eventName.Contains("."))
                        continue;

                    lineCount++;
                    RememberSecrets(eventName, evt, secrets);
                    eventNames.Add(eventName);

                    foreach (var property in evt.Properties())
                    {
                        if (property.Name != "event")
                            Flatten("journal." + eventName + "." + property.Name, property.Value, types);
                    }
                }
            }

            var result = new List<string>();
            rejected = 0;
            foreach (var eventName in eventNames.Where(e => !libraryEvents.Contains(e)))
                result.Add("event\t" + eventName);

            foreach (var pair in types.Where(p => !knownKeys.Contains(p.Key)).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (secrets.Any(s => pair.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    rejected++;
                    continue;
                }

                result.Add(pair.Key + "\t" + pair.Value);
            }

            return result;
        }

        public static void Merge(Catalog catalog, IEnumerable<string> lines)
        {
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length != 2)
                    throw new InvalidDataException(FileName + ": invalid line '" + line + "'");

                if (parts[0] == "event")
                {
                    // the game's spelling wins (DropshipDeploy, not DropShipDeploy)
                    var prefix = "journal." + parts[1];
                    var group = catalog.FindByPrefix(prefix);
                    if (group == null)
                        EventCatalog.EventGroup(catalog, parts[1]);
                    else
                    {
                        group.Id = prefix;
                        group.Prefix = prefix;
                        group.Label = parts[1];
                    }

                    continue;
                }

                var key = parts[0];
                int eventEnd = key.IndexOf('.', "journal.".Length);
                if (!key.StartsWith("journal.", StringComparison.Ordinal) || eventEnd < 0)
                    throw new InvalidDataException(FileName + ": invalid key '" + key + "'");

                var eventGroup = EventCatalog.EventGroup(catalog, key.Substring("journal.".Length, eventEnd - "journal.".Length));
                eventGroup.Add(key.Substring(eventEnd + 1), parts[1]);
            }
        }

        private static void RememberSecrets(string eventName, JObject evt, HashSet<string> secrets)
        {
            string[] fields;
            if (eventName == "Commander")
                fields = new[] { "Name", "FID" };
            else if (eventName == "LoadGame")
                fields = new[] { "Commander", "FID" };
            else
                return;

            foreach (var field in fields)
            {
                var value = evt.Value<string>(field);
                if (!string.IsNullOrEmpty(value) && value.Length >= 3)
                    secrets.Add(value);
            }
        }

        // same rules as Elite.Generic.StoreKeys
        private static void Flatten(string key, JToken value, Dictionary<string, string> types)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                    foreach (var property in ((JObject)value).Properties())
                        Flatten(key + "." + property.Name, property.Value, types);
                    break;
                case JTokenType.Array:
                    SetType(types, key + ".#count", FieldTypes.Number);
                    break;
                case JTokenType.Null:
                case JTokenType.Undefined:
                    break;
                case JTokenType.Boolean:
                    SetType(types, key, FieldTypes.Bool);
                    break;
                case JTokenType.Integer:
                case JTokenType.Float:
                    SetType(types, key, FieldTypes.Number);
                    break;
                case JTokenType.Date:
                    SetType(types, key, FieldTypes.Date);
                    break;
                default:
                    SetType(types, key, FieldTypes.Text);
                    break;
            }
        }

        private static void SetType(Dictionary<string, string> types, string key, string type)
        {
            string existing;
            if (!types.TryGetValue(key, out existing))
                types[key] = type;
            else if (existing != type)
                types[key] = FieldTypes.Text; // mixed types: display as text
        }
    }
}
