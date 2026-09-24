using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EliteJournalReader.Events;

namespace Elite.CatalogGen
{
    /// <summary>
    /// status.json keys, following Elite.Generic.StoreKeys.FromStatus: status.Flags.X / status.Flags2.X (booleans),
    /// status.Pips.System/Engine/Weapons, status.GuiFocus (enum name), other fields with their JSON path.
    /// </summary>
    internal static class StatusCatalog
    {
        private const string Prefix = "status";

        // C# property name -> JSON name, when they differ
        private static readonly Dictionary<string, string> JsonNames = new Dictionary<string, string>
        {
            { "Firegroup", "FireGroup" },
        };

        // the library types these as strings, the game writes numbers (system address, body id)
        private static readonly Dictionary<string, string> TypeOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Destination.System", FieldTypes.Integer },
            { "Destination.Body", FieldTypes.Integer },
        };

        private static readonly HashSet<string> OdysseyFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Oxygen", "Health", "Temperature", "SelectedWeapon", "SelectedWeapon_Localised", "Gravity"
        };

        // documented values of LegalState (Frontier journal documentation)
        private static readonly List<string> LegalStates = new List<string>
        {
            "Clean", "IllegalCargo", "Speeding", "Wanted", "Hostile", "PassengerWanted", "Warrant"
        };

        public static void AddTo(Catalog catalog)
        {
            catalog.Groups.Add(FlagsGroup(Prefix + ".Flags", "Flags", Categories.StatusFlags, false, typeof(StatusFlags)));
            catalog.Groups.Add(FlagsGroup(Prefix + ".Flags2", "Flags2", Categories.StatusFlags2, true, typeof(MoreStatusFlags)));

            var values = new Group(Prefix, Prefix, "Status.json", Categories.StatusValues, false);
            var odyssey = new Group(Prefix + ".odyssey", Prefix, "Status.json (Odyssey)", Categories.StatusValues, true);
            catalog.Groups.Add(values);
            catalog.Groups.Add(odyssey);

            values.Add("timestamp", FieldTypes.Date);
            catalog.Enums["LegalState"] = LegalStates;

            foreach (var property in typeof(StatusFileEvent).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetSetMethod(false) == null)
                    continue;

                var name = property.Name;
                string jsonName;
                if (JsonNames.TryGetValue(name, out jsonName))
                    name = jsonName;

                var group = OdysseyFields.Contains(name) ? odyssey : values;
                switch (name)
                {
                    case "Timestamp":
                    case "Flags":
                    case "Flags2":
                        break;
                    case "Pips":
                        values.Add("Pips.System", FieldTypes.Integer);
                        values.Add("Pips.Engine", FieldTypes.Integer);
                        values.Add("Pips.Weapons", FieldTypes.Integer);
                        break;
                    case "GuiFocus":
                        values.Add(name, FieldTypes.Enum, catalog.RegisterEnum(typeof(StatusGuiFocus)));
                        break;
                    case "LegalState":
                        values.Add(name, FieldTypes.Enum, "LegalState");
                        break;
                    default:
                        AddValue(group, name, property.PropertyType);
                        break;
                }
            }

            // official field, missing from the library
            odyssey.Add("SelectedWeapon_Localised", FieldTypes.Text);
        }

        private static Group FlagsGroup(string prefix, string label, string category, bool odyssey, Type enumType)
        {
            var group = new Group(prefix, prefix, label, category, odyssey);
            foreach (var flag in Enum.GetValues(enumType))
            {
                if (Convert.ToInt64(flag) == 0)
                    continue; // None
                group.Add(Enum.GetName(enumType, flag), FieldTypes.Bool);
            }

            return group;
        }

        private static void AddValue(Group group, string path, Type type)
        {
            string overridden;
            if (TypeOverrides.TryGetValue(path, out overridden))
            {
                group.Add(path, overridden);
                return;
            }

            if (type == typeof(bool))
                group.Add(path, FieldTypes.Bool);
            else if (type == typeof(string))
                group.Add(path, FieldTypes.Text);
            else if (type.IsPrimitive || type == typeof(decimal))
                group.Add(path, EventCatalog.NumberType(type));
            else
            {
                // StatusFuel, Destination
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetSetMethod(false) != null))
                    AddValue(group, path + "." + property.Name, property.PropertyType);
            }
        }
    }
}
