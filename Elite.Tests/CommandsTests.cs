using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Elite\PropertyInspector\commands.js: the commands EliteKeys.SendKeypress can send (decision D2).
    /// </summary>
    [TestFixture]
    public class CommandsTests
    {
        private Dictionary<string, List<string>> commandsByGroup;

        [OneTimeSetUp]
        public void Load()
        {
            var commands = TestData.GeneratedJs("commands.js", "ELITE_COMMANDS");
            commandsByGroup = commands["groups"].ToDictionary(
                g => (string)g["id"],
                g => g["commands"].Select(c => (string)c[0]).ToList());
        }

        [Test]
        public void Groups_AndCounts()
        {
            Assert.That(commandsByGroup.Keys, Is.EqualTo(new[] { "ship", "srv", "onfoot", "general", "smart" }));
            Assert.That(commandsByGroup.Values.All(c => c.Count > 0), Is.True);
            Assert.That(commandsByGroup.Values.Sum(c => c.Count), Is.EqualTo(366));
            Assert.That(commandsByGroup["smart"].Count, Is.EqualTo(30));
            Assert.That(commandsByGroup.Values.SelectMany(c => c), Is.Unique);
        }

        [Test]
        public void EveryPlainCommand_IsAUserBinding()
        {
            var notBindings = commandsByGroup.Where(g => g.Key != "smart")
                .SelectMany(g => g.Value)
                .Where(name =>
                {
                    var property = typeof(UserBindings).GetProperty(name);
                    return property == null || !typeof(StandardBindingInfo).IsAssignableFrom(property.PropertyType);
                })
                .ToList();

            Assert.That(notBindings, Is.Empty);
        }

        [Test]
        public void Examples_InTheRightGroup()
        {
            Assert.That(commandsByGroup["ship"], Does.Contain("CycleFireGroupNext"));
            Assert.That(commandsByGroup["srv"].Any(c => c.Contains("Buggy")), Is.True);
            Assert.That(commandsByGroup["onfoot"].Any(c => c.StartsWith("Humanoid")), Is.True);
            Assert.That(commandsByGroup["general"], Does.Contain("UI_Up"));
            Assert.That(commandsByGroup["smart"], Does.Contain("LandingGearToggle-ON"));
            Assert.That(commandsByGroup["smart"], Does.Contain("FireGroup-A"));
            Assert.That(commandsByGroup.Values.SelectMany(c => c), Does.Not.Contain("SelectTargetBuggy"), "not handled by EliteKeys.SendKeypress");
        }
    }
}
