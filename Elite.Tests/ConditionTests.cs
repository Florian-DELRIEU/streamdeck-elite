using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class ConditionTests
    {
        private static bool Test(JToken value, string op, string operand = "")
        {
            return Condition.Evaluate(value, Condition.ParseOperator(op), operand);
        }

        [Test]
        public void ParseOperator_InspectorNames()
        {
            Assert.That(Condition.ParseOperator("isTrue"), Is.EqualTo(ConditionOperator.IsTrue));
            Assert.That(Condition.ParseOperator("notEquals"), Is.EqualTo(ConditionOperator.NotEqualTo));
            Assert.That(Condition.ParseOperator("gte"), Is.EqualTo(ConditionOperator.GreaterOrEqual));
            Assert.That(Condition.ParseOperator(""), Is.EqualTo(ConditionOperator.None));
            Assert.That(Condition.ParseOperator("nimportequoi"), Is.EqualTo(ConditionOperator.None));
        }

        [Test]
        public void AbsentValue_NeverMatches()
        {
            Assert.That(Test(null, "isFalse"), Is.False);
            Assert.That(Test(null, "notEquals", "x"), Is.False);
            Assert.That(Test(JValue.CreateNull(), "lt", "10"), Is.False);
            Assert.That(Test(new JValue(true), "", ""), Is.False, "no test");
        }

        [Test]
        public void TrueFalse()
        {
            Assert.That(Test(new JValue(true), "isTrue"), Is.True);
            Assert.That(Test(new JValue(false), "isFalse"), Is.True);
            Assert.That(Test(new JValue(0), "isFalse"), Is.True);
            Assert.That(Test(new JValue(2.5), "isTrue"), Is.True);
            Assert.That(Test(new JValue("Clean"), "isTrue"), Is.True);
            Assert.That(Test(new JValue(""), "isFalse"), Is.True);
        }

        [Test]
        public void Equality_NumbersTextAndBooleans()
        {
            Assert.That(Test(new JValue(0.5), "equals", "0,5"), Is.True, "comma accepted");
            Assert.That(Test(new JValue(8), "equals", "8"), Is.True);
            Assert.That(Test(new JValue("GalaxyMap"), "equals", "galaxymap"), Is.True, "case-insensitive");
            Assert.That(Test(new JValue("GalaxyMap"), "notEquals", "SystemMap"), Is.True);
            Assert.That(Test(new JValue(true), "equals", "oui"), Is.True);
            Assert.That(Test(new JValue(false), "equals", "true"), Is.False);
            Assert.That(Test(new JValue(true), "equals", "peut-etre"), Is.False);
        }

        [Test]
        public void Ordering_NumbersOnly()
        {
            Assert.That(Test(new JValue(12.0), "lt", "25"), Is.True);
            Assert.That(Test(new JValue(25), "lte", "25"), Is.True);
            Assert.That(Test(new JValue(25), "lt", "25"), Is.False);
            Assert.That(Test(new JValue(30.5), "gt", "30,4"), Is.True);
            Assert.That(Test(new JValue(-1), "gte", "0"), Is.False);
            Assert.That(Test(new JValue("Wanted"), "gt", "1"), Is.False, "text cannot be ordered");
            Assert.That(Test(new JValue(5), "lt", "abc"), Is.False, "invalid threshold");
        }
    }
}
