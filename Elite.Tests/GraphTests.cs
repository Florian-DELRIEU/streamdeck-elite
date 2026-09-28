using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elite.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// "Graphique" action (v3.4, docs/L10-v3.md): settings of a view, bounds, colours, drawing.
    /// </summary>
    [TestFixture]
    public class GraphTests
    {
        [Test]
        public void Settings_Defaults_AndAView()
        {
            var empty = GraphConfig.Parse(new JObject(), "");
            Assert.That(empty.Type, Is.EqualTo(GraphType.HorizontalBar));
            Assert.That(empty.Min, Is.EqualTo("0"));
            Assert.That(empty.Max, Is.EqualTo("100"));
            Assert.That(empty.Color, Is.EqualTo(GraphConfig.DefaultColor));
            Assert.That(empty.Rules, Is.Empty);

            var view2 = GraphConfig.Parse(JObject.Parse(@"{ ""graphType2"":""curve"", ""graphMin2"":"""", ""graphMax2"":""ship.FuelCapacity.Main"",
                ""colorRule1Op2"":""lt"", ""colorRule1Value2"":""25"", ""colorRule1Color2"":""#ff4646"", ""colorRule3Op2"":""gte"", ""colorRule3Value2"":""90"",
                ""curvePoints2"":""1000"", ""curveInterval2"":""0"" }"), "2");
            Assert.That(view2.Type, Is.EqualTo(GraphType.Curve));
            Assert.That(view2.Max, Is.EqualTo("ship.FuelCapacity.Main"));
            Assert.That(view2.Rules.Count, Is.EqualTo(2), "rules without a test are ignored");
            Assert.That(view2.Rules[1].Color, Is.EqualTo(GraphConfig.DefaultRuleColor), "default colour of a rule");
            Assert.That(view2.CurvePoints, Is.EqualTo(History.MaxPoints), "clamped");
            Assert.That(view2.CurveInterval, Is.EqualTo(History.MinInterval), "clamped");
        }

        [Test]
        public void Bounds_ANumberOrAKey_InTheDisplayedUnit()
        {
            var store = new Dictionary<string, JToken> { { "ship.FuelCapacity.Main", new JValue(32.0) } };
            Func<string, JToken> read = k => store.ContainsKey(k) ? store[k] : null;
            var display = ValueSettings.Parse("", "", "0", "*100/32", "0", false, null, null);

            Assert.That(GraphConfig.Bound("10", read, display), Is.EqualTo(10), "a number is already in the displayed unit");
            Assert.That(GraphConfig.Bound("ship.FuelCapacity.Main", read, display), Is.EqualTo(100).Within(0.0001), "a key goes through the factor");
            Assert.That(GraphConfig.Bound("", read, display), Is.Null);
            Assert.That(GraphConfig.Bound("unknown.key", read, display), Is.Null);
            Assert.That(GraphConfig.BoundKeys(new GraphView { Min = "0", Max = "ship.FuelCapacity.Main" }), Is.EqualTo(new[] { "ship.FuelCapacity.Main" }));
        }

        [Test]
        public void Fraction_AndColourRules()
        {
            Assert.That(GraphConfig.Fraction(16, 0, 32), Is.EqualTo(0.5));
            Assert.That(GraphConfig.Fraction(50, 0, 32), Is.EqualTo(1), "clamped");
            Assert.That(GraphConfig.Fraction(null, 0, 32), Is.EqualTo(0));
            Assert.That(GraphConfig.Fraction(5, 10, 10), Is.EqualTo(0), "wrong bounds");

            var view = new GraphView { Color = "#a7d7d6" };
            view.Rules.Add(new ColorRule { Operator = ConditionOperator.LessThan, Operand = "25", Color = "#ff4646" });
            view.Rules.Add(new ColorRule { Operator = ConditionOperator.LessThan, Operand = "50", Color = "#d18105" });
            Assert.That(GraphConfig.ColorOf(view, new JValue(10)), Is.EqualTo("#ff4646"), "the first true rule wins");
            Assert.That(GraphConfig.ColorOf(view, new JValue(40)), Is.EqualTo("#d18105"));
            Assert.That(GraphConfig.ColorOf(view, new JValue(80)), Is.EqualTo("#a7d7d6"));
        }

        [Test]
        public void EveryType_IsDrawn()
        {
            var points = Enumerable.Range(0, 30).Select(i => (double?)Math.Sin(i / 4.0) * 10).ToList();
            points[12] = null; // a hole in the history
            foreach (GraphType type in Enum.GetValues(typeof(GraphType)))
            {
                var base64 = KeyRenderer.RenderBase64(null, g => GraphRenderer.Draw(g, type, 0.6, "#a7d7d6", points, null, null), "Fuel 60 %", new TextStyle { Size = 14, Position = TextPosition.Top });
                Assert.That(base64, Does.StartWith("data:image/png;base64,"), type.ToString());
            }
        }

        /// <summary>Visual check: PNG of the 4 types into ZV_RENDER_PREVIEW (run on demand, see KeyRendererTests).</summary>
        [Test, Explicit]
        public void WriteGraphPreviews()
        {
            var folder = Environment.GetEnvironmentVariable("ZV_RENDER_PREVIEW");
            Assert.That(folder, Is.Not.Null.And.Not.Empty, "set ZV_RENDER_PREVIEW");
            Directory.CreateDirectory(folder);

            var points = Enumerable.Range(0, 60).Select(i => (double?)(32 - i * 0.3 + Math.Sin(i / 3.0))).ToList();
            var style = new TextStyle { Size = 14, Position = TextPosition.Top };
            Save(folder, "g1-barre", KeyRenderer.RenderBase64(null, g => GraphRenderer.Draw(g, GraphType.HorizontalBar, 0.72, "#a7d7d6", null, 0, 32), "Fuel 72 %", style));
            Save(folder, "g2-vertical", KeyRenderer.RenderBase64(null, g => GraphRenderer.Draw(g, GraphType.VerticalBar, 0.2, "#ff4646", null, 0, 32), "Soute 20 %", style));
            Save(folder, "g3-cadran", KeyRenderer.RenderBase64(null, g => GraphRenderer.Draw(g, GraphType.Dial, 0.55, "#d18105", null, 0, 100), "Coque 55", style));
            Save(folder, "g4-courbe", KeyRenderer.RenderBase64(null, g => GraphRenderer.Draw(g, GraphType.Curve, 0, "#a7d7d6", points, null, null), "Fuel", style));
        }

        private static void Save(string folder, string name, string base64)
        {
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), Convert.FromBase64String(base64.Substring(base64.IndexOf(',') + 1)));
        }
    }
}
