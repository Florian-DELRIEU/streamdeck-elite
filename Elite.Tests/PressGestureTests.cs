using Elite.Generic;
using NUnit.Framework;

namespace Elite.Tests
{
    [TestFixture]
    public class PressGestureTests
    {
        [Test]
        public void ParseMode_DefaultIsShortActLongView()
        {
            Assert.That(PressGesture.ParseMode(null), Is.EqualTo(PressMode.ShortActLongView));
            Assert.That(PressGesture.ParseMode(""), Is.EqualTo(PressMode.ShortActLongView));
            Assert.That(PressGesture.ParseMode("shortActLongView"), Is.EqualTo(PressMode.ShortActLongView));
            Assert.That(PressGesture.ParseMode("shortViewLongAct"), Is.EqualTo(PressMode.ShortViewLongAct));
        }

        [TestCase(false, PressMode.ShortActLongView, 3, PressOutcome.Act)]
        [TestCase(true, PressMode.ShortActLongView, 3, PressOutcome.NextView)]
        [TestCase(false, PressMode.ShortViewLongAct, 3, PressOutcome.NextView)]
        [TestCase(true, PressMode.ShortViewLongAct, 3, PressOutcome.Act)]
        public void Resolve_BothModes(bool isLong, PressMode mode, int views, PressOutcome expected)
        {
            Assert.That(PressGesture.Resolve(isLong, mode, views), Is.EqualTo(expected));
        }

        [TestCase(false, PressMode.ShortActLongView)]
        [TestCase(true, PressMode.ShortActLongView)]
        [TestCase(false, PressMode.ShortViewLongAct)]
        [TestCase(true, PressMode.ShortViewLongAct)]
        public void SingleView_AlwaysActs(bool isLong, PressMode mode)
        {
            Assert.That(PressGesture.Resolve(isLong, mode, 1), Is.EqualTo(PressOutcome.Act));
        }
    }
}
