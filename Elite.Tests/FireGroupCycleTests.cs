using System.Collections.Generic;
using Elite.Generic;
using NUnit.Framework;

namespace Elite.Tests
{
    /// <summary>
    /// Closed loop of the fire groups (v3.8, D10: in the SRV, only group B worked because "previous fire group" had no
    /// key). The game is simulated: number of groups, wrap after the last group, keys bound or not.
    /// </summary>
    [TestFixture]
    public class FireGroupCycleTests
    {
        private sealed class Game
        {
            public int Groups = 2;
            public int Current;
            public bool Wraps = true;
            public bool NextBound = true;
            public bool PreviousBound = true;
            public bool Responds = true;
            public readonly List<string> Presses = new List<string>();

            public FireGroupResult Run(int target)
            {
                return FireGroupCycle.Run(target, () => Current, NextBound, PreviousBound, Press, before => Current == before ? (int?)null : Current);
            }

            private void Press(bool next)
            {
                Presses.Add(next ? "next" : "previous");
                if (!Responds)
                    return;
                if (next)
                    Current = Current + 1 < Groups ? Current + 1 : Wraps ? 0 : Current;
                else
                    Current = Current > 0 ? Current - 1 : Wraps ? Groups - 1 : Current;
            }
        }

        [Test]
        public void SrvOfD10_WithoutPrevious_GoesBackToAWithNext()
        {
            var game = new Game { Current = 1, PreviousBound = false };
            var result = game.Run(0);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.Reached));
            Assert.That(game.Current, Is.EqualTo(0));
            Assert.That(game.Presses, Is.EqualTo(new[] { "next" }), "B -> (wrap) A");
        }

        [Test]
        public void PreviousIsUsedWhenItIsShorterAndBound()
        {
            var game = new Game { Groups = 5, Current = 3 };
            var result = game.Run(1);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.Reached));
            Assert.That(game.Presses, Is.EqualTo(new[] { "previous", "previous" }));
        }

        [Test]
        public void NextGoesForward()
        {
            var game = new Game { Groups = 5, Current = 0 };
            var result = game.Run(3);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.Reached));
            Assert.That(result.Presses, Is.EqualTo(3));
            Assert.That(game.Current, Is.EqualTo(3));
        }

        [Test]
        public void GroupThatDoesNotExist_StopsWhenBackToTheStart()
        {
            var game = new Game { Groups = 2, Current = 0 };
            var result = game.Run(2); // C on an SRV with 2 groups
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.NotFound));
            Assert.That(game.Current, Is.EqualTo(0), "back where it started");
            Assert.That(result.Presses, Is.EqualTo(2));
        }

        [Test]
        public void NoWrapAfterTheLastGroup_IsNoResponse()
        {
            var game = new Game { Groups = 2, Current = 1, PreviousBound = false, Wraps = false };
            var result = game.Run(0);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.NoResponse));
            Assert.That(result.Presses, Is.EqualTo(1));
        }

        [Test]
        public void GameThatDoesNotAnswer_StopsAfterOnePress()
        {
            var game = new Game { Groups = 4, Current = 0, Responds = false };
            var result = game.Run(2);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.NoResponse));
            Assert.That(game.Presses.Count, Is.EqualTo(1));
        }

        [Test]
        public void GroupAlreadyActive_SendsNothing()
        {
            var game = new Game { Current = 1 };
            Assert.That(game.Run(1).Outcome, Is.EqualTo(FireGroupOutcome.AlreadyActive));
            Assert.That(game.Presses, Is.Empty);
        }

        [Test]
        public void NoKeyAtAll_SendsNothing()
        {
            var game = new Game { Current = 0, NextBound = false, PreviousBound = false };
            Assert.That(game.Run(1).Outcome, Is.EqualTo(FireGroupOutcome.NoBinding));
            Assert.That(game.Presses, Is.Empty);
        }

        [Test]
        public void OnlyPreviousBound_GoesForwardByWrapping()
        {
            var game = new Game { Groups = 3, Current = 0, NextBound = false };
            var result = game.Run(1);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.Reached));
            Assert.That(game.Presses, Is.EqualTo(new[] { "previous", "previous" }), "A -> (wrap) C -> B");
        }

        [Test]
        public void NeverMoreThanEightPresses()
        {
            var game = new Game { Groups = 100, Current = 0, PreviousBound = false };
            var result = game.Run(7);
            Assert.That(result.Outcome, Is.EqualTo(FireGroupOutcome.Reached));
            Assert.That(game.Presses.Count, Is.LessThanOrEqualTo(FireGroupCycle.MaxPresses));
        }
    }
}
