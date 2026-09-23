using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EliteJournalReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elite.CatalogGen
{
    /// <summary>
    /// Journal events, by reflection over the JournalEvent classes of EliteJournalReader.
    /// Keys follow the rules of Elite.Generic.StoreKeys: objects give dotted paths, arrays give "#count".
    /// </summary>
    internal static class EventCatalog
    {
        private const int MaxDepth = 4;

        public static void AddTo(Catalog catalog)
        {
            var eventTypes = typeof(JournalEvent).Assembly.GetTypes()
                .Where(t => typeof(JournalEvent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition);

            foreach (var type in eventTypes)
            {
                var argsType = EventArgsType(type);
                if (argsType == null)
                    continue;

                var handler = (JournalEvent)Activator.CreateInstance(type);
                foreach (var eventName in handler.EventNames)
                {
                    if (eventName.StartsWith("MagicMau.", StringComparison.Ordinal))
                        continue; // synthetic event of the library, not written by the game

                    var group = EventGroup(catalog, eventName);
                    Walk(catalog, group, "", argsType, 0, new HashSet<Type>());
                }
            }
        }

        public static Group EventGroup(Catalog catalog, string eventName)
        {
            var prefix = "journal." + eventName;
            var group = catalog.FindByPrefix(prefix);
            if (group == null)
            {
                group = new Group(prefix, prefix, eventName, Categories.ForEvent(eventName), Categories.IsOdysseyEvent(eventName));
                group.Add("timestamp", FieldTypes.Date);
                catalog.Groups.Add(group);
            }

            return group;
        }

        private static Type EventArgsType(Type eventType)
        {
            for (var baseType = eventType.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(JournalEvent<>))
                    return baseType.GetGenericArguments()[0];
            }

            return null;
        }

        private static void Walk(Catalog catalog, Group group, string prefix, Type type, int depth, HashSet<Type> visiting)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.DeclaringType == typeof(JournalEventArgs) || property.DeclaringType == typeof(EventArgs))
                    continue; // OriginalEvent, Timestamp
                if (property.GetIndexParameters().Length > 0 || property.GetSetMethod(false) == null)
                    continue; // computed properties are not in the JSON
                if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Any())
                    continue;

                var converter = property.GetCustomAttributes(typeof(JsonConverterAttribute), true).Cast<JsonConverterAttribute>().FirstOrDefault();
                AddProperty(catalog, group, prefix + property.Name, property.PropertyType, converter, depth, visiting);
            }
        }

        private static void AddProperty(Catalog catalog, Group group, string path, Type propertyType, JsonConverterAttribute converter, int depth, HashSet<Type> visiting)
        {
            var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (converter != null && !converter.ConverterType.Name.StartsWith("ExtendedStringEnumConverter", StringComparison.Ordinal))
            {
                // custom converters of the library read JSON arrays (StarPos, Parents, Modifiers...)
                group.Add(path + ".#count", FieldTypes.Number);
                return;
            }

            if (type == typeof(bool))
                group.Add(path, FieldTypes.Bool);
            else if (type == typeof(string))
                group.Add(path, FieldTypes.Text);
            else if (type == typeof(DateTime))
                group.Add(path, FieldTypes.Date);
            else if (type.IsEnum)
                group.Add(path, FieldTypes.Enum, catalog.RegisterEnum(type));
            else if (type.IsPrimitive || type == typeof(decimal))
                group.Add(path, FieldTypes.Number);
            else if (typeof(JToken).IsAssignableFrom(type) || IsDictionary(type))
                return; // unknown shape
            else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
                group.Add(path + ".#count", FieldTypes.Number);
            else if (depth < MaxDepth && !visiting.Contains(type))
            {
                visiting.Add(type);
                Walk(catalog, group, path + ".", type, depth + 1, visiting);
                visiting.Remove(type);
            }
        }

        private static bool IsDictionary(Type type)
        {
            return typeof(IDictionary).IsAssignableFrom(type)
                || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }
    }
}
