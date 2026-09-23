using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Elite.CatalogGen
{
    internal static class FieldTypes
    {
        public const string Bool = "bool";
        public const string Number = "number";
        public const string Text = "text";
        public const string Date = "date";
        public const string Enum = "enum";
    }

    internal class Category
    {
        public Category(string id, string label, string emoji)
        {
            Id = id;
            Label = label;
            Emoji = emoji;
        }

        public string Id { get; }
        public string Label { get; }
        public string Emoji { get; }
    }

    internal class Field
    {
        public Field(string path, string type, string enumName)
        {
            Path = path;
            Type = type;
            EnumName = enumName;
        }

        public string Path { get; }
        public string Type { get; }
        public string EnumName { get; }
    }

    /// <summary>
    /// A set of keys sharing a prefix: one journal event ("journal.FSDJump") or one part of the status.
    /// Full key = Prefix + "." + field path.
    /// </summary>
    internal class Group
    {
        public Group(string id, string prefix, string label, string category, bool odyssey)
        {
            Id = id;
            Prefix = prefix;
            Label = label;
            Category = category;
            Odyssey = odyssey;
        }

        public string Id { get; set; }
        public string Prefix { get; set; }
        public string Label { get; set; }
        public string Category { get; }
        public bool Odyssey { get; }
        public Dictionary<string, Field> Fields { get; } = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);

        public void Add(string path, string type, string enumName = null)
        {
            if (!Fields.ContainsKey(path))
                Fields[path] = new Field(path, type, enumName);
        }
    }

    internal class Catalog
    {
        public List<Group> Groups { get; } = new List<Group>();

        // enum name -> values as written by the game
        public SortedDictionary<string, List<string>> Enums { get; } = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);

        private readonly Dictionary<Type, string> enumNames = new Dictionary<Type, string>();

        public Group FindByPrefix(string prefix)
        {
            return Groups.FirstOrDefault(g => string.Equals(g.Prefix, prefix, StringComparison.OrdinalIgnoreCase));
        }

        public int KeyCount
        {
            get { return Groups.Sum(g => g.Fields.Count); }
        }

        public IEnumerable<string> AllKeys()
        {
            return Groups.SelectMany(g => g.Fields.Keys.Select(path => g.Prefix + "." + path));
        }

        /// <summary>
        /// Registers an enum type and returns its name in the catalog. Values are the [Description] when present
        /// (that is what the game writes, e.g. "Metal rich body"), otherwise the member name.
        /// </summary>
        public string RegisterEnum(Type enumType)
        {
            string name;
            if (enumNames.TryGetValue(enumType, out name))
                return name;

            var values = enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f =>
                {
                    var description = f.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                    return description != null ? description.Description : f.Name;
                })
                .ToList();

            name = enumType.Name;
            for (int i = 2; Enums.ContainsKey(name) && !Enums[name].SequenceEqual(values); i++)
                name = enumType.Name + i;

            Enums[name] = values;
            enumNames[enumType] = name;
            return name;
        }
    }
}
