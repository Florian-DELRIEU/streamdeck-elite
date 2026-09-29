using System;
using System.Collections.Generic;
using System.Linq;

namespace Elite.CatalogGen
{
    /// <summary>
    /// French names shown in the menus of the property inspector (v3.8, docs/L11-v3.8.md): labels-fr.json, merged into
    /// catalog.js ("labels"). Key: a full key ("status.Flags.Docked") or a group id ("journal.FSDJump", "status.Flags").
    /// Only what is displayed changes: the settings keep the technical key. Checked by LabelsTests.
    /// </summary>
    internal static class Labels
    {
        public const string FrenchFile = "labels-fr.json";

        /// <summary>Adds the labels of existing keys and groups (catalog spelling); returns the ones that match nothing.</summary>
        public static List<string> Merge(Catalog catalog, IDictionary<string, string> labels)
        {
            var keys = catalog.AllKeys().Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);
            var groups = catalog.Groups.GroupBy(g => g.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Key, StringComparer.OrdinalIgnoreCase);
            var orphans = new List<string>();

            foreach (var pair in labels)
            {
                string canonical;
                if (keys.TryGetValue(pair.Key, out canonical) || groups.TryGetValue(pair.Key, out canonical))
                    catalog.Labels[canonical] = pair.Value;
                else
                    orphans.Add(pair.Key);
            }

            return orphans;
        }
    }
}
