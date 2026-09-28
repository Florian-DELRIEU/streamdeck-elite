namespace Elite.CatalogGen
{
    /// <summary>
    /// Keys produced by the plugin itself (docs/L10-v3.md): calc.* (Elite.Generic.DerivedKeys, BackpackTracker) and
    /// ship.* (Elite.Generic.ShipMemory). Keep in step with those classes; every key needs a French description
    /// (descriptions-fr.json, checked by DescriptionsTests).
    /// </summary>
    internal static class PluginCatalog
    {
        // consumables of the Odyssey backpack (other items: key typed by hand, calc.Backpack.<name>)
        private static readonly string[] Consumables = { "healthpack", "energycell", "amm_grenade_emp", "amm_grenade_frag", "amm_grenade_shield", "bypass" };

        public static void AddTo(Catalog catalog)
        {
            var route = new Group("calc.Route", "calc.Route", "Calculé — route", "travel", false);
            route.Add("RemainingJumps", FieldTypes.Integer);
            catalog.Groups.Add(route);

            var computed = new Group("calc.ship", "calc", "Calculé — vaisseau", "ship", false);
            computed.Add("Fuel.Percent", FieldTypes.Number);
            computed.Add("Cargo.Percent", FieldTypes.Number);
            computed.Add("Hull.Percent", FieldTypes.Number);
            computed.Add("Jump.Range", FieldTypes.Number);
            computed.Add("Fsd.Supercharged", FieldTypes.Bool);
            catalog.Groups.Add(computed);

            var backpack = new Group("calc.Backpack", "calc.Backpack", "Calculé — sac à dos", Categories.OnFoot, true);
            foreach (var name in Consumables)
                backpack.Add(name, FieldTypes.Integer);
            catalog.Groups.Add(backpack);

            var ship = new Group("ship", "ship", "Vaisseau mémorisé", "ship", false);
            ship.Add("ShipID", FieldTypes.Integer);
            ship.Add("Ship", FieldTypes.Text);
            ship.Add("ShipName", FieldTypes.Text);
            ship.Add("ShipIdent", FieldTypes.Text);
            ship.Add("CargoCapacity", FieldTypes.Integer);
            ship.Add("FuelCapacity.Main", FieldTypes.Number);
            ship.Add("FuelCapacity.Reserve", FieldTypes.Number);
            ship.Add("MaxJumpRange", FieldTypes.Number);
            ship.Add("HullHealth", FieldTypes.Number);
            ship.Add("UnladenMass", FieldTypes.Number);
            ship.Add("FsdOvercharge", FieldTypes.Bool);
            ship.Add("Fsd.Module", FieldTypes.Text);
            ship.Add("Fsd.OptimalMass", FieldTypes.Number);
            ship.Add("Fsd.MaxFuelPerJump", FieldTypes.Number);
            ship.Add("Fsd.FuelMultiplier", FieldTypes.Number);
            ship.Add("Fsd.FuelPower", FieldTypes.Number);
            ship.Add("GuardianBoost", FieldTypes.Number);
            catalog.Groups.Add(ship);
        }
    }
}
