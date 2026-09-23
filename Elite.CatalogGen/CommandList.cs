using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Elite.CatalogGen
{
    internal class CommandGroup
    {
        public CommandGroup(string id, string label)
        {
            Id = id;
            Label = label;
        }

        public string Id { get; }
        public string Label { get; }

        // (name sent to EliteKeys.SendKeypress, label)
        public List<KeyValuePair<string, string>> Commands { get; } = new List<KeyValuePair<string, string>>();
    }

    /// <summary>
    /// Keyboard commands (decision D2): the case labels of EliteKeys.SendKeypress(string), i.e. exactly what the plugin
    /// can send, grouped by the binding file they use (BindingType). Labels with a suffix (LandingGearToggle-ON,
    /// FireGroup-A...) depend on the game state and form their own group.
    /// </summary>
    internal static class CommandList
    {
        private const string Signature = "public static void SendKeypress(string function)";
        private const string SmartGroup = "smart";

        private static readonly Regex CaseLabel = new Regex("case \"([^\"]+)\":", RegexOptions.Compiled);
        private static readonly Regex BindingTypeUse = new Regex(@"BindingType\.(\w+)", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> GroupOfBindingType = new Dictionary<string, string>
        {
            { "Ship", "ship" },
            { "Srv", "srv" },
            { "OnFoot", "onfoot" },
            { "General", "general" },
        };

        public static List<CommandGroup> Parse(string eliteKeysSource)
        {
            var groups = new List<CommandGroup>
            {
                new CommandGroup("ship", "Vaisseau"),
                new CommandGroup("srv", "SRV"),
                new CommandGroup("onfoot", "À pied"),
                new CommandGroup("general", "Général"),
                new CommandGroup(SmartGroup, "Selon l'état du jeu"),
            };

            int start = eliteKeysSource.IndexOf(Signature, StringComparison.Ordinal);
            if (start < 0)
                throw new InvalidOperationException("EliteKeys.cs: '" + Signature + "' not found");

            int end = eliteKeysSource.IndexOf("public static void", start + Signature.Length, StringComparison.Ordinal);
            var body = end < 0 ? eliteKeysSource.Substring(start) : eliteKeysSource.Substring(start, end - start);

            var labels = CaseLabel.Matches(body).Cast<Match>().ToList();
            for (int i = 0; i < labels.Count; i++)
            {
                int blockEnd = i + 1 < labels.Count ? labels[i + 1].Index : body.Length;
                var block = body.Substring(labels[i].Index, blockEnd - labels[i].Index);
                var name = labels[i].Groups[1].Value;

                string groupId;
                if (name.Contains("-"))
                    groupId = SmartGroup;
                else
                {
                    var bindingTypes = BindingTypeUse.Matches(block).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
                    if (bindingTypes.Count != 1 || !GroupOfBindingType.TryGetValue(bindingTypes[0], out groupId))
                        throw new InvalidOperationException("EliteKeys.cs: unexpected binding types for '" + name + "': " + string.Join(",", bindingTypes));
                }

                groups.First(g => g.Id == groupId).Commands.Add(new KeyValuePair<string, string>(name, Humanize(name)));
            }

            foreach (var group in groups)
                group.Commands.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));

            return groups;
        }

        /// <summary>
        /// "CycleFireGroupNext" -> "Cycle Fire Group Next", "UI_Up" -> "UI Up", "LandingGearToggle-ON" -> "Landing Gear Toggle (ON)".
        /// </summary>
        public static string Humanize(string name)
        {
            string suffix = null;
            int dash = name.IndexOf('-');
            if (dash > 0)
            {
                suffix = name.Substring(dash + 1);
                name = name.Substring(0, dash);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_')
                {
                    sb.Append(' ');
                    continue;
                }

                bool boundary = i > 0 && char.IsUpper(c)
                    && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])
                        || (char.IsUpper(name[i - 1]) && i + 1 < name.Length && char.IsLower(name[i + 1])));
                if (boundary && sb.Length > 0 && sb[sb.Length - 1] != ' ')
                    sb.Append(' ');
                sb.Append(c);
            }

            var label = Regex.Replace(sb.ToString(), " +", " ").Trim();
            return suffix == null ? label : label + " (" + suffix + ")";
        }
    }
}
