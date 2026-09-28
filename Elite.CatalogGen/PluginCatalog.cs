namespace Elite.CatalogGen
{
    /// <summary>
    /// Keys produced by the plugin itself (docs/L10-v3.md): calc.* (Elite.Generic.DerivedKeys) and ship.*
    /// (Elite.Generic.ShipMemory). Keep in step with those classes; every key needs a French description
    /// (descriptions-fr.json, checked by DescriptionsTests).
    /// </summary>
    internal static class PluginCatalog
    {
        public static void AddTo(Catalog catalog)
        {
            var route = new Group("calc.Route", "calc.Route", "Calculé — route", "travel", false);
            route.Add("RemainingJumps", FieldTypes.Integer);
            catalog.Groups.Add(route);
        }
    }
}
