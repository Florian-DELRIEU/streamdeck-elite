using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.CatalogGen
{
    /// <summary>
    /// Descriptions shown by the "i" button of the property inspector (docs/L6-retours-d5.md).
    /// - "descriptions" mode: extracts the English comments of EliteJournalReader/Events/*.cs ("When written" and
    ///   "Parameters" blocks) into descriptions-en.json, the source of the translation;
    /// - build: descriptions-fr.json (translation + status written in French) is merged into catalog.js ("info").
    /// Keys: an event ("journal.FSDJump": when it is written) or a full key ("journal.FSDJump.JumpDist", "status.Flags.Docked").
    /// </summary>
    internal static class Descriptions
    {
        public const string EnglishFile = "descriptions-en.json";
        public const string FrenchFile = "descriptions-fr.json";

        private static readonly Regex EventName = new Regex("base\\(\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex NamedField = new Regex(@"^([A-Za-z_][A-Za-z0-9_]*)\s*(?::|-|=)\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex LooseField = new Regex(@"^([A-Za-z_][A-Za-z0-9_]*)\s+(.+)$", RegexOptions.Compiled);

        public static SortedDictionary<string, string> ExtractEnglish(string eventsDirectory, Catalog catalog)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(eventsDirectory, "*.cs").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var source = File.ReadAllText(file, Encoding.UTF8);
                var name = EventName.Match(source);
                var group = name.Success ? catalog.FindByPrefix("journal." + name.Groups[1].Value) : null;
                int classStart = source.IndexOf("public class", StringComparison.Ordinal);
                if (group == null || classStart < 0)
                    continue;

                var when = new StringBuilder();
                bool inWhen = false, inParameters = false;
                string lastField = null;
                foreach (var rawLine in source.Substring(0, classStart).Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (!line.StartsWith("//", StringComparison.Ordinal))
                        continue;

                    var content = line.Substring(2);
                    var text = Clean(content);
                    if (text.StartsWith("When written", StringComparison.OrdinalIgnoreCase))
                    {
                        inWhen = true;
                        inParameters = false;
                        when.Append(text.Substring(text.IndexOf(':') + 1).Trim());
                        continue;
                    }

                    if (text.StartsWith("Parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        inWhen = false;
                        inParameters = true;
                        continue;
                    }

                    if (inWhen)
                    {
                        if (text.Length == 0)
                            inWhen = false;
                        else
                            when.Append(' ').Append(text);
                        continue;
                    }

                    if (!inParameters || text.Length == 0)
                        continue;

                    // nested field: "o Name" under the previous field
                    bool nested = Regex.IsMatch(content.TrimStart('\t', ' ', '�', '•'), @"^o\s");
                    if (nested)
                        text = Regex.Replace(text, @"^o\s+", "");

                    var match = NamedField.Match(text);
                    string fieldName = null, description = null;
                    if (match.Success)
                    {
                        fieldName = match.Groups[1].Value;
                        description = match.Groups[2].Value.Trim();
                    }
                    else
                    {
                        match = LooseField.Match(text);
                        if (match.Success)
                        {
                            fieldName = match.Groups[1].Value;
                            description = match.Groups[2].Value.Trim();
                        }
                    }

                    if (fieldName == null)
                        continue;

                    var path = nested && lastField != null ? lastField + "." + fieldName : fieldName;
                    if (!nested)
                        lastField = fieldName;

                    Field field;
                    if (description.Length > 0 && group.Fields.TryGetValue(path, out field))
                        result[group.Prefix + "." + field.Path] = description;
                }

                var whenText = when.ToString().Trim();
                if (whenText.Length > 0)
                    result[group.Prefix] = whenText;
            }

            return result;
        }

        /// <summary>
        /// Adds the descriptions whose key is an event or a field of the catalog (with the catalog spelling).
        /// Returns the keys that match nothing (checked by the unit tests).
        /// </summary>
        public static List<string> Merge(Catalog catalog, IDictionary<string, string> descriptions)
        {
            var prefixes = catalog.Groups.GroupBy(g => g.Prefix, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Key, StringComparer.OrdinalIgnoreCase);
            var keys = catalog.AllKeys().Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);
            var orphans = new List<string>();

            foreach (var pair in descriptions)
            {
                string canonical;
                if (keys.TryGetValue(pair.Key, out canonical) || prefixes.TryGetValue(pair.Key, out canonical))
                    catalog.Info[canonical] = pair.Value;
                else
                    orphans.Add(pair.Key);
            }

            return orphans;
        }

        public static Dictionary<string, string> Load(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path))
                return result;

            foreach (var property in JObject.Parse(File.ReadAllText(path, Encoding.UTF8)).Properties())
            {
                if (!property.Name.StartsWith("#", StringComparison.Ordinal)) // "#comment" entries
                    result[property.Name] = (string)property.Value;
            }

            return result;
        }

        public static string ToJson(IDictionary<string, string> descriptions, string comment)
        {
            var json = new JObject { { "#comment", comment } };
            foreach (var pair in descriptions.OrderBy(p => p.Key, StringComparer.Ordinal))
                json[pair.Key] = pair.Value;
            return json.ToString(Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
        }

        private static string Clean(string text)
        {
            text = text.Replace('�', ' ').Replace('•', ' ').Replace('·', ' ').Replace('\t', ' ');
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text.TrimStart('-', '*').Trim();
        }
    }
}
