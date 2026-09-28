using System.Collections.Generic;

namespace Elite.Generic
{
    public class FsdData
    {
        public FsdData(double optimalMass, double maxFuelPerJump, double fuelMultiplier, double fuelPower)
        {
            OptimalMass = optimalMass;
            MaxFuelPerJump = maxFuelPerJump;
            FuelMultiplier = fuelMultiplier;
            FuelPower = fuelPower;
        }

        public double OptimalMass { get; }
        public double MaxFuelPerJump { get; }
        public double FuelMultiplier { get; }
        public double FuelPower { get; }
    }

    /// <summary>
    /// Frame shift drives and Guardian FSD boosters, for the jump range with the current mass (calc.Jump.Range, v3.3).
    /// Game data of Elite Dangerous, copyright Frontier Developments plc, as published by EDCD in
    /// https://github.com/EDCD/coriolis-data (modules/standard/frame_shift_drive.json, modules/internal/
    /// guardian_fsd_booster.json, 2026-09-28): optmass, maxfuel, fuelmul, fuelpower, jumpboost only.
    /// Checked against the MaxJumpRange of 6 real Loadout events (docs/L10-v3.md). Key = module name in lower case; the pre-engineered duplicates of the source are left out.
    /// </summary>
    public static class FsdTables
    {
        public static readonly Dictionary<string, FsdData> Drives = new Dictionary<string, FsdData>
        {
            { "int_hyperdrive_overcharge_size2_class1", new FsdData(60.0, 0.6, 0.008, 2.0) },
            { "int_hyperdrive_overcharge_size2_class2", new FsdData(90.0, 0.9, 0.012, 2.0) },
            { "int_hyperdrive_overcharge_size2_class3", new FsdData(90.0, 0.9, 0.012, 2.0) },
            { "int_hyperdrive_overcharge_size2_class4", new FsdData(90.0, 0.9, 0.012, 2.0) },
            { "int_hyperdrive_overcharge_size2_class5", new FsdData(100.0, 1.0, 0.013, 2.0) },
            { "int_hyperdrive_overcharge_size3_class1", new FsdData(100.0, 1.2, 0.008, 2.15) },
            { "int_hyperdrive_overcharge_size3_class2", new FsdData(150.0, 1.8, 0.012, 2.15) },
            { "int_hyperdrive_overcharge_size3_class3", new FsdData(150.0, 1.8, 0.012, 2.15) },
            { "int_hyperdrive_overcharge_size3_class4", new FsdData(150.0, 1.8, 0.012, 2.15) },
            { "int_hyperdrive_overcharge_size3_class5", new FsdData(167.0, 1.9, 0.013, 2.15) },
            { "int_hyperdrive_overcharge_size4_class1", new FsdData(350.0, 2.0, 0.008, 2.3) },
            { "int_hyperdrive_overcharge_size4_class2", new FsdData(525.0, 3.0, 0.012, 2.3) },
            { "int_hyperdrive_overcharge_size4_class3", new FsdData(525.0, 3.0, 0.012, 2.3) },
            { "int_hyperdrive_overcharge_size4_class4", new FsdData(525.0, 3.0, 0.012, 2.3) },
            { "int_hyperdrive_overcharge_size4_class5", new FsdData(585.0, 3.2, 0.013, 2.3) },
            { "int_hyperdrive_overcharge_size5_class1", new FsdData(700.0, 3.3, 0.008, 2.45) },
            { "int_hyperdrive_overcharge_size5_class2", new FsdData(1050.0, 5.0, 0.012, 2.45) },
            { "int_hyperdrive_overcharge_size5_class3", new FsdData(1050.0, 5.0, 0.012, 2.45) },
            { "int_hyperdrive_overcharge_size5_class4", new FsdData(1050.0, 5.0, 0.012, 2.45) },
            { "int_hyperdrive_overcharge_size5_class5", new FsdData(1175.0, 5.2, 0.013, 2.45) },
            { "int_hyperdrive_overcharge_size6_class1", new FsdData(1200.0, 5.3, 0.008, 2.6) },
            { "int_hyperdrive_overcharge_size6_class2", new FsdData(1800.0, 8.0, 0.012, 2.6) },
            { "int_hyperdrive_overcharge_size6_class3", new FsdData(1800.0, 8.0, 0.012, 2.6) },
            { "int_hyperdrive_overcharge_size6_class4", new FsdData(1800.0, 8.0, 0.012, 2.6) },
            { "int_hyperdrive_overcharge_size6_class5", new FsdData(2000.0, 8.3, 0.013, 2.6) },
            { "int_hyperdrive_overcharge_size7_class1", new FsdData(1800.0, 8.5, 0.008, 2.75) },
            { "int_hyperdrive_overcharge_size7_class2", new FsdData(2700.0, 12.8, 0.012, 2.75) },
            { "int_hyperdrive_overcharge_size7_class3", new FsdData(2700.0, 12.8, 0.012, 2.75) },
            { "int_hyperdrive_overcharge_size7_class4", new FsdData(2700.0, 12.8, 0.012, 2.75) },
            { "int_hyperdrive_overcharge_size7_class5", new FsdData(3000.0, 13.1, 0.013, 2.75) },
            { "int_hyperdrive_overcharge_size8_class1", new FsdData(2800.0, 13.6, 0.008, 2.9) },
            { "int_hyperdrive_overcharge_size8_class2", new FsdData(4200.0, 20.4, 0.012, 2.9) },
            { "int_hyperdrive_overcharge_size8_class3", new FsdData(4200.0, 20.4, 0.012, 2.9) },
            { "int_hyperdrive_overcharge_size8_class4", new FsdData(4200.0, 20.4, 0.012, 2.9) },
            { "int_hyperdrive_overcharge_size8_class5", new FsdData(4670.0, 20.7, 0.013, 2.9) },
            { "int_hyperdrive_overcharge_size8_class5_overchargebooster_mkii", new FsdData(4670.0, 6.8, 0.011, 2.5025) },
            { "int_hyperdrive_size2_class1", new FsdData(48.0, 0.6, 0.011, 2.0) },
            { "int_hyperdrive_size2_class2", new FsdData(54.0, 0.6, 0.01, 2.0) },
            { "int_hyperdrive_size2_class3", new FsdData(60.0, 0.6, 0.008, 2.0) },
            { "int_hyperdrive_size2_class4", new FsdData(75.0, 0.8, 0.01, 2.0) },
            { "int_hyperdrive_size2_class5", new FsdData(90.0, 0.9, 0.012, 2.0) },
            { "int_hyperdrive_size3_class1", new FsdData(80.0, 1.2, 0.011, 2.15) },
            { "int_hyperdrive_size3_class2", new FsdData(90.0, 1.2, 0.01, 2.15) },
            { "int_hyperdrive_size3_class3", new FsdData(100.0, 1.2, 0.008, 2.15) },
            { "int_hyperdrive_size3_class4", new FsdData(125.0, 1.5, 0.01, 2.15) },
            { "int_hyperdrive_size3_class5", new FsdData(150.0, 1.8, 0.012, 2.15) },
            { "int_hyperdrive_size4_class1", new FsdData(280.0, 2.0, 0.011, 2.3) },
            { "int_hyperdrive_size4_class2", new FsdData(315.0, 2.0, 0.01, 2.3) },
            { "int_hyperdrive_size4_class3", new FsdData(350.0, 2.0, 0.008, 2.3) },
            { "int_hyperdrive_size4_class4", new FsdData(437.5, 2.5, 0.01, 2.3) },
            { "int_hyperdrive_size4_class5", new FsdData(525.0, 3.0, 0.012, 2.3) },
            { "int_hyperdrive_size5_class1", new FsdData(560.0, 3.3, 0.011, 2.45) },
            { "int_hyperdrive_size5_class2", new FsdData(630.0, 3.3, 0.01, 2.45) },
            { "int_hyperdrive_size5_class3", new FsdData(700.0, 3.3, 0.008, 2.45) },
            { "int_hyperdrive_size5_class4", new FsdData(875.0, 4.1, 0.01, 2.45) },
            { "int_hyperdrive_size5_class5", new FsdData(1050.0, 5.0, 0.012, 2.45) },
            { "int_hyperdrive_size6_class1", new FsdData(960.0, 5.3, 0.011, 2.6) },
            { "int_hyperdrive_size6_class2", new FsdData(1080.0, 5.3, 0.01, 2.6) },
            { "int_hyperdrive_size6_class3", new FsdData(1200.0, 5.3, 0.008, 2.6) },
            { "int_hyperdrive_size6_class4", new FsdData(1500.0, 6.6, 0.01, 2.6) },
            { "int_hyperdrive_size6_class5", new FsdData(1800.0, 8.0, 0.012, 2.6) },
            { "int_hyperdrive_size7_class1", new FsdData(1440.0, 8.5, 0.011, 2.75) },
            { "int_hyperdrive_size7_class2", new FsdData(1620.0, 8.5, 0.01, 2.75) },
            { "int_hyperdrive_size7_class3", new FsdData(1800.0, 8.5, 0.008, 2.75) },
            { "int_hyperdrive_size7_class4", new FsdData(2250.0, 10.6, 0.01, 2.75) },
            { "int_hyperdrive_size7_class5", new FsdData(2700.0, 12.8, 0.012, 2.75) },
            { "int_missing_hyperdrive", new FsdData(0.0, 0.0, 0.0, 0.0) },
        };

        public static readonly Dictionary<string, double> GuardianBoosters = new Dictionary<string, double>
        {
            { "int_guardianfsdbooster_size1", 4.0 },
            { "int_guardianfsdbooster_size2", 6.0 },
            { "int_guardianfsdbooster_size3", 7.75 },
            { "int_guardianfsdbooster_size4", 9.25 },
            { "int_guardianfsdbooster_size5", 10.5 },
        };
    }
}
