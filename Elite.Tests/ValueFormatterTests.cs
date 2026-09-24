using System;
using System.Collections.Generic;
using System.Globalization;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class ValueFormatterTests
    {
        private static readonly DateTime Now = new DateTime(2026, 9, 23, 15, 0, 0, DateTimeKind.Local);

        private static string Format(JToken value, ValueSettings settings = null)
        {
            return ValueFormatter.Format(value, settings ?? new ValueSettings(), Now);
        }

        [Test]
        public void Absent_ShowsEmptyText_WithPrefixAndLineBreak()
        {
            Assert.That(Format(null), Is.EqualTo("—"));
            Assert.That(Format(JValue.CreateNull()), Is.EqualTo("—"));
            Assert.That(Format(null, new ValueSettings { Prefix = "Fuel\\n" }), Is.EqualTo("Fuel\n—"));
            Assert.That(Format(null, new ValueSettings { EmptyText = "" }), Is.EqualTo(""));
        }

        [Test]
        public void Booleans_TextAndEnums()
        {
            Assert.That(Format(new JValue(true)), Is.EqualTo("Oui"));
            Assert.That(Format(new JValue(false)), Is.EqualTo("Non"));
            Assert.That(Format(new JValue("Two Ladies")), Is.EqualTo("Two Ladies"));
            Assert.That(Format(new JValue("GalaxyMap"), new ValueSettings { Prefix = "[", Suffix = "]" }), Is.EqualTo("[GalaxyMap]"));
        }

        [Test]
        public void Numbers_FrenchFormat_Decimals()
        {
            Assert.That(Format(new JValue(27.5), new ValueSettings { Decimals = 1 }), Is.EqualTo("27,5"));
            Assert.That(Format(new JValue(12345678L)), Is.EqualTo("12 345 678"));
            Assert.That(Format(new JValue(0.126), new ValueSettings { Decimals = 2 }), Is.EqualTo("0,13"));
            Assert.That(Format(new JValue(-3)), Is.EqualTo("-3"));
        }

        [Test]
        public void Numbers_ScaleAndOffset()
        {
            // body temperature: K -> degrees C
            Assert.That(Format(new JValue(293.15), new ValueSettings { Offset = -273.15, Decimals = 1, Suffix = " °C" }), Is.EqualTo("20,0 °C"));
            // half pips -> pips
            Assert.That(Format(new JValue(8), new ValueSettings { Scale = 0.5 }), Is.EqualTo("4"));
        }

        [TestCase(12345678, "12,3 M")]
        [TestCase(123456789, "123 M")]
        [TestCase(1500000000, "1,5 G")]
        [TestCase(1234, "1,23 k")]
        [TestCase(999, "999")]
        [TestCase(999950, "1 M")]
        [TestCase(-12345678, "-12,3 M")]
        public void Numbers_Compact_ThreeSignificantDigits(long value, string expected)
        {
            Assert.That(Format(new JValue(value), new ValueSettings { Compact = true }), Is.EqualTo(expected));
        }

        [Test]
        public void Dates_LocalTime_TodayOrWithDate()
        {
            var today = Now.AddMinutes(-30).ToUniversalTime();
            Assert.That(Format(new JValue(today)), Is.EqualTo(Now.AddMinutes(-30).ToString("HH:mm", CultureInfo.InvariantCulture)));

            var older = Now.AddDays(-3).ToUniversalTime();
            Assert.That(Format(new JValue(older)), Is.EqualTo(Now.AddDays(-3).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)));
        }

        [Test]
        public void Settings_ParsedFromInspectorStrings()
        {
            var warnings = new List<string>();
            var settings = ValueSettings.Parse("Fuel\\n", " t", "10", "0,5", "abc", true, null, warnings.Add);

            Assert.That(settings.Decimals, Is.EqualTo(6), "clamped to 0..6");
            Assert.That(settings.Scale, Is.EqualTo(0.5), "comma accepted");
            Assert.That(settings.Offset, Is.EqualTo(0), "invalid -> default");
            Assert.That(settings.EmptyText, Is.EqualTo("—"));
            Assert.That(settings.Compact, Is.True);
            Assert.That(warnings.Count, Is.EqualTo(1));

            var defaults = ValueSettings.Parse(null, null, "", " ", null, false, "", warnings.Add);
            Assert.That(defaults.Decimals, Is.EqualTo(0));
            Assert.That(defaults.Scale, Is.EqualTo(1));
            Assert.That(defaults.EmptyText, Is.EqualTo(""), "cleared by the user");
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [TestCase("/32", 1.0 / 32)]
        [TestCase("*4", 4.0)]
        [TestCase("x2", 2.0)]
        [TestCase("100/32", 3.125)]
        [TestCase("*100/32", 3.125)]
        [TestCase(" 0,5 ", 0.5)]
        [TestCase("-1", -1.0)]
        [TestCase("", 1.0)]
        public void Scale_NumbersAndOperations(string text, double expected)
        {
            Assert.That(ValueSettings.ParseScale(text), Is.EqualTo(expected).Within(1e-12));
        }

        [TestCase("/0")]
        [TestCase("abc")]
        [TestCase("2*")]
        [TestCase("*")]
        [TestCase("4+1")]
        public void Scale_Invalid_GivesOneAndWarns(string text)
        {
            var warnings = new List<string>();
            Assert.That(ValueSettings.ParseScale(text, warnings.Add), Is.EqualTo(1));
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Scale_FuelPercentage()
        {
            // 24 t in a 32 t tank, shown as a percentage
            Assert.That(Format(new JValue(24.0), new ValueSettings { Scale = ValueSettings.ParseScale("*100/32"), Suffix = " %" }), Is.EqualTo("75 %"));
        }

        [Test]
        public void RealStoreValue_IsFormatted()
        {
            EliteStore.Reset();
            try
            {
                var line = TestData.JournalLine("FSDJump");
                var evt = JObject.Parse(line);
                EliteStore.HandleRawJournal(null, new EliteJournalReader.RawJournalEventArgs("FSDJump", evt, false));

                JToken value;
                EliteStore.TryGet("journal.FSDJump.JumpDist", out value);
                Assert.That(Format(value, new ValueSettings { Decimals = 1, Suffix = " al" }), Is.EqualTo("19,1 al"));
            }
            finally
            {
                EliteStore.Reset();
            }
        }
    }
}
