namespace Elite.CatalogGen
{
    /// <summary>
    /// Keys produced by the plugin itself (docs/L10-v3.md, docs/L11-v3.8.md): calc.* (Elite.Generic.DerivedKeys,
    /// BackpackTracker), ship.* (Elite.Generic.ShipMemory), system.* and target.* (Elite.Generic.SystemTracker).
    /// Keep in step with those classes; every key needs a French description (descriptions-fr.json, checked by
    /// DescriptionsTests) and a French name (labels-fr.json, checked by LabelsTests).
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
            // v3.8: illegal items (Elite.Generic.IllegalItems + stolen items)
            backpack.Add("Illegal", FieldTypes.Bool);
            backpack.Add("IllegalCount", FieldTypes.Integer);
            backpack.Add("Stolen", FieldTypes.Integer);
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

            // v3.8 (docs/L11-v3.8.md): Elite.Generic.SystemTracker / SystemMemory
            var system = new Group("system", "system", "Système actuel", "travel", false);
            AddSystemFields(system);
            catalog.Groups.Add(system);

            var target = new Group("target", "target", "Système cible", "travel", false);
            target.Add("Known", FieldTypes.Bool);
            target.Add("LastVisit", FieldTypes.Date);
            target.Add("RemainingJumps", FieldTypes.Integer);
            AddSystemFields(target);
            catalog.Groups.Add(target);
        }

        // SystemTracker.Summarize
        private static void AddSystemFields(Group group)
        {
            group.Add("Name", FieldTypes.Text);
            group.Add("Address", FieldTypes.Integer);
            group.Add("Population", FieldTypes.Integer);
            group.Add("Allegiance", FieldTypes.Text);
            group.Add("Government", FieldTypes.Text);
            group.Add("Economy", FieldTypes.Text);
            group.Add("SecondEconomy", FieldTypes.Text);
            group.Add("Security", FieldTypes.Text);
            group.Add("Power", FieldTypes.Text);
            group.Add("PowerplayState", FieldTypes.Text);
            group.Add("Wanted", FieldTypes.Bool);
            group.Add("Faction.Name", FieldTypes.Text);
            group.Add("Faction.State", FieldTypes.Text);
            group.Add("Faction.Influence", FieldTypes.Number);
            group.Add("Faction.Happiness", FieldTypes.Text);
            group.Add("Faction.Reputation", FieldTypes.Number);
            group.Add("Faction.Relation", FieldTypes.Text);
            group.Add("Faction.Government", FieldTypes.Text);
            group.Add("Faction.Allegiance", FieldTypes.Text);
            group.Add("Conflicts", FieldTypes.Integer);
            group.Add("Conflict.Type", FieldTypes.Text);
            group.Add("Conflict.Status", FieldTypes.Text);
            group.Add("Conflict.Faction", FieldTypes.Text);
            group.Add("Conflict.Opponent", FieldTypes.Text);
            group.Add("Conflict.WonDays", FieldTypes.Integer);
            group.Add("Conflict.OpponentWonDays", FieldTypes.Integer);
            group.Add("Star.Class", FieldTypes.Text);
            group.Add("Star.Scoopable", FieldTypes.Bool);
        }
    }
}
