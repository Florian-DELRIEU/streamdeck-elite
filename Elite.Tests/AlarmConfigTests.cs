using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Settings of the Alarm key and its filter (docs/L8-alarme.md), with real journal lines of journal-extrait.txt.
    /// </summary>
    [TestFixture]
    public class AlarmConfigTests
    {
        private static AlarmConfig Config(string json)
        {
            return AlarmConfig.FromSettings(JObject.Parse(json));
        }

        private static JObject Line(string eventName, int occurrence = 0)
        {
            return JObject.Parse(TestData.JournalLine(eventName, occurrence));
        }

        [Test]
        public void EmptySettings_Defaults()
        {
            var config = AlarmConfig.FromSettings(null);

            Assert.That(config.Event, Is.Empty);
            Assert.That(config.HasFilter, Is.False);
            Assert.That(config.DurationSeconds, Is.EqualTo(5));
            Assert.That(config.Matches("UnderAttack", Line("UnderAttack")), Is.False, "no event chosen: never triggers");
        }

        [Test]
        public void SettingsOfThePage_AreRead()
        {
            // settings as sent by EventAlarm.html (empty file fields are sent as "No file...")
            var config = Config(@"{ ""event"":""UnderAttack"", ""filterField"":""Target"", ""filterOp"":""equals"", ""filterValue"":""You"",
                ""duration"":""0"", ""idleImage"":""No file..."", ""activeImage"":""C:\\icons\\attack.png"", ""alarmSound"":""No file..."",
                ""pressCommand"":""FireChaffLauncher"", ""pressHotkey"":""ControlLeft+F5"", ""pressHotkeyText"":""Ctrl+F5"", ""clickSound"":"""" }");

            Assert.That(config.Event, Is.EqualTo("UnderAttack"));
            Assert.That(config.FilterField, Is.EqualTo("Target"));
            Assert.That(config.FilterOperator, Is.EqualTo(ConditionOperator.EqualTo));
            Assert.That(config.FilterValue, Is.EqualTo("You"));
            Assert.That(config.HasFilter, Is.True);
            Assert.That(config.DurationSeconds, Is.EqualTo(0), "0 = until a key press");
            Assert.That(config.IdleImage, Is.Empty, "\"No file...\" = no image");
            Assert.That(config.ActiveImage, Is.EqualTo(@"C:\icons\attack.png"));
            Assert.That(config.AlarmSound, Is.Empty);
            Assert.That(config.Command, Is.EqualTo("FireChaffLauncher"));
            Assert.That(config.Hotkey, Is.EqualTo("ControlLeft+F5"));
            Assert.That(config.HotkeyText, Is.EqualTo("Ctrl+F5"));
        }

        [Test]
        public void Duration_InvalidOrNegative_GivesFiveSecondsAndAWarning()
        {
            string warning = null;
            Assert.That(AlarmConfig.FromSettings(JObject.Parse(@"{ ""duration"":""abc"" }"), w => warning = w).DurationSeconds, Is.EqualTo(5));
            Assert.That(warning, Does.Contain("abc"));

            Assert.That(Config(@"{ ""duration"":""-3"" }").DurationSeconds, Is.EqualTo(5));
            Assert.That(Config(@"{ ""duration"":"""" }").DurationSeconds, Is.EqualTo(5), "empty field = default");
            Assert.That(Config(@"{ ""duration"":""2,5"" }").DurationSeconds, Is.EqualTo(2.5), "comma accepted");
            Assert.That(Config(@"{ ""duration"":12 }").DurationSeconds, Is.EqualTo(12), "number accepted");
        }

        [Test]
        public void IncompleteFilter_IsNoFilter()
        {
            Assert.That(Config(@"{ ""event"":""UnderAttack"", ""filterField"":""Target"" }").HasFilter, Is.False, "field without test");
            Assert.That(Config(@"{ ""event"":""UnderAttack"", ""filterOp"":""equals"", ""filterValue"":""You"" }").HasFilter, Is.False, "test without field");
            Assert.That(Config(@"{ ""event"":""UnderAttack"", ""filterField"":""Target"" }").Matches("UnderAttack", Line("UnderAttack", 0)), Is.True);
        }

        [Test]
        public void Filter_UnderAttackOnYou()
        {
            var you = Config(@"{ ""event"":""UnderAttack"", ""filterField"":""Target"", ""filterOp"":""equals"", ""filterValue"":""you"" }");

            Assert.That(you.Matches("UnderAttack", Line("UnderAttack", 1)), Is.True, "Target You (case ignored)");
            Assert.That(you.Matches("UnderAttack", Line("UnderAttack", 0)), Is.False, "Target Mothership");

            var notYou = Config(@"{ ""event"":""UnderAttack"", ""filterField"":""Target"", ""filterOp"":""notEquals"", ""filterValue"":""You"" }");
            Assert.That(notYou.Matches("UnderAttack", Line("UnderAttack", 0)), Is.True);
        }

        [Test]
        public void NoFilter_EveryEventOfTheType_AndOnlyThatType()
        {
            var config = Config(@"{ ""event"":""UnderAttack"" }");

            Assert.That(config.Matches("UnderAttack", Line("UnderAttack", 0)), Is.True);
            Assert.That(config.Matches("UnderAttack", Line("UnderAttack", 1)), Is.True);
            Assert.That(config.Matches("HullDamage", Line("HullDamage")), Is.False);
            Assert.That(config.Matches("UnderAttack", null), Is.False);
        }

        [Test]
        public void Filter_Number()
        {
            // real line: HullDamage with a Health between 0 and 1
            var evt = Line("HullDamage");
            double health = (double)evt["Health"];

            Assert.That(Config(@"{ ""event"":""HullDamage"", ""filterField"":""Health"", ""filterOp"":""lt"", ""filterValue"":""" + (health + 0.01).ToString(System.Globalization.CultureInfo.InvariantCulture) + @""" }")
                .Matches("HullDamage", evt), Is.True);
            Assert.That(Config(@"{ ""event"":""HullDamage"", ""filterField"":""Health"", ""filterOp"":""lt"", ""filterValue"":""" + (health - 0.01).ToString(System.Globalization.CultureInfo.InvariantCulture) + @""" }")
                .Matches("HullDamage", evt), Is.False);
        }

        [Test]
        public void Filter_AbsentField_NeverMatches()
        {
            var config = Config(@"{ ""event"":""UnderAttack"", ""filterField"":""NoSuchField"", ""filterOp"":""isFalse"" }");

            Assert.That(config.Matches("UnderAttack", Line("UnderAttack", 1)), Is.False);
        }

        [Test]
        public void EventName_CaseIgnored_AndFullKeysAccepted()
        {
            // the game writes DropshipDeploy, the library declares DropShipDeploy
            Assert.That(Config(@"{ ""event"":""DropShipDeploy"" }").Matches("DropshipDeploy", Line("DropshipDeploy")), Is.True);

            // keys typed by hand as in a Data key
            var config = Config(@"{ ""event"":""journal.UnderAttack"", ""filterField"":""journal.UnderAttack.Target"", ""filterOp"":""equals"", ""filterValue"":""You"" }");
            Assert.That(config.Event, Is.EqualTo("UnderAttack"));
            Assert.That(config.FilterField, Is.EqualTo("Target"));
            Assert.That(config.Matches("UnderAttack", Line("UnderAttack", 1)), Is.True);
        }
    }
}
