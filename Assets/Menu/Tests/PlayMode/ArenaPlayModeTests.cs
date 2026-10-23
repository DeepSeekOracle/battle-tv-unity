using System.Collections;
using BattleTv.Fight;
using BattleTv.Menu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BattleTv.Menu.PlayTests
{
    public sealed class ArenaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ArenaRuntimeShowsOnboardingHint()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);

            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var found = false;
            foreach (var text in texts)
            {
                if (text.text == ArenaView.OnboardingLine)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Arena runtime did not create the onboarding hint '" + ArenaView.OnboardingLine + "'.");
        }

        [UnityTest]
        public IEnumerator ArenaMatchRunsToCompletionAndShowsBoard()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);

            var steps = 0;
            while (!ArenaMatch.Over && steps < 20000)
            {
                ArenaMatch.Step(0.05f);
                steps++;
                if (steps % 200 == 0)
                    yield return null;
            }

            Assert.IsTrue(ArenaMatch.Over, "Arena did not finish within 20000 steps.");
            yield return null;
            yield return new WaitForSeconds(0.5f);

            var board = GameObject.Find("Scoreboard");
            Assert.IsNotNull(board, "Scoreboard was not created.");
            Assert.IsTrue(board.activeSelf, "Scoreboard did not activate at match end.");
            Assert.IsFalse(string.IsNullOrEmpty(ArenaMatch.Title), "Scoreboard title is empty.");
            Assert.IsFalse(string.IsNullOrEmpty(ArenaMatch.Detail), "Scoreboard detail is empty.");
        }

        [UnityTest]
        public IEnumerator WolfAssetsLoadAndAnimate()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            yield return FighterBodies.Load(new[] { "wolf" });
            var body = FighterBodies.Get("wolf");
            Assert.IsNotNull(body, "wolf body did not load");
            Assert.IsNotNull(body.Idle, "wolf idle missing");
            var walk = body.Find("walk");
            Assert.IsNotNull(walk, "wolf walk clip missing");
            Assert.GreaterOrEqual(walk.Sprites.Length, 3, "wolf walk needs 3 frames");
            var bite = body.Find("Bite");
            Assert.IsNotNull(bite, "wolf bite clip missing");
            Assert.GreaterOrEqual(bite.Sprites.Length, 2, "wolf bite needs 2 frames");
            Assert.IsNotNull(body.Find("Roar"), "wolf roar clip missing");
            Assert.IsNotNull(body.End(false), "wolf death clip missing");
            Assert.IsNotNull(body.End(true), "wolf win clip missing");
        }

        [UnityTest]
        public IEnumerator AllMissingRosterArtLoads()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            var ids = new[] { "heartboss", "levi", "tithe", "unnamer", "lock", "lion", "bear", "elephant", "scorpion" };
            yield return FighterBodies.Load(ids);
            foreach (var id in ids)
            {
                var body = FighterBodies.Get(id);
                Assert.IsNotNull(body, id + " body did not load");
                Assert.IsNotNull(body.Idle, id + " idle missing");
                var walk = body.Find("walk");
                Assert.IsNotNull(walk, id + " walk clip missing");
                Assert.GreaterOrEqual(walk.Sprites.Length, 3, id + " walk needs 3 frames");
                var bite = body.Find("Bite");
                Assert.IsNotNull(bite, id + " attack clip missing");
                Assert.GreaterOrEqual(bite.Sprites.Length, 2, id + " attack needs 2 frames");
                Assert.IsNotNull(body.Find("Roar"), id + " roar clip missing");
                Assert.IsNotNull(body.End(false), id + " death clip missing");
                Assert.IsNotNull(body.End(true), id + " win clip missing");
            }
        }

        [UnityTest]
        public IEnumerator EveryCharacterWorksIn1v1()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            var ids = new string[LaneRoster.All.Length];
            for (var i = 0; i < ids.Length; i++)
                ids[i] = LaneRoster.All[i].Id;
            yield return FighterBodies.Load(ids);
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var foe = id == "vale" ? "brute" : "vale";
                ArenaMatch.Begin(new[] { id }, new[] { foe }, 1000 + i, true);
                var hitsBefore = ArenaLogic.Hits;
                var castsBefore = ArenaLogic.Casts;
                var steps = 0;
                while (!ArenaMatch.Over && steps < 20000)
                {
                    ArenaMatch.Step(0.05);
                    steps++;
                    if (steps % 500 == 0)
                        yield return null;
                }
                Assert.IsTrue(ArenaMatch.Over, id + " 1v1 did not finish");
                Assert.Less(ArenaMatch.Time, SideDuel.HardStop, id + " 1v1 only ended on the hard stop");
                Assert.Greater(ArenaLogic.Hits - hitsBefore, 0, id + " 1v1 landed no hits");
                Assert.Greater(ArenaLogic.Casts - castsBefore, 0, id + " 1v1 never used a move");
                Assert.IsNotNull(FighterBodies.Get(id), id + " body missing in 1v1");
            }
        }

        [UnityTest]
        public IEnumerator FiveVersusFiveBodiesStaySeparated()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            ArenaMatch.Begin(
                new[] { "vale", "kael", "orin", "nia", "lyra" },
                new[] { "brute", "gate", "crown", "smith", "drain" },
                77, false);
            for (var i = 0; i < 120; i++)
            {
                ArenaMatch.Step(0.05);
                if (i % 20 == 0)
                    yield return null;
            }
            var min = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                if (!ArenaMatch.Bodies[i].Alive)
                    continue;
                for (var j = i + 1; j < ArenaMatch.Bodies.Count; j++)
                {
                    if (!ArenaMatch.Bodies[j].Alive)
                        continue;
                    var dx = ArenaMatch.Bodies[i].X - ArenaMatch.Bodies[j].X;
                    var dy = ArenaMatch.Bodies[i].Y - ArenaMatch.Bodies[j].Y;
                    var d = System.Math.Sqrt(dx * dx + dy * dy);
                    if (d < min)
                        min = d;
                }
            }
            Assert.Greater(min, 28, "5v5 bodies collapsed into one stack");
        }

        [UnityTest]
        public IEnumerator PinnedFighterEscapesCorner()
        {
            SceneManager.LoadScene("Arena");
            yield return null;
            yield return new WaitForSeconds(1.5f);
            ArenaMatch.Begin(new[] { "vale" }, new[] { "brute" }, 99, true);
            var vale = ArenaMatch.Bodies[0];
            var brute = ArenaMatch.Bodies[1];
            vale.X = ArenaMatch.Inset + 2;
            vale.Y = ArenaMatch.WorldH * 0.5;
            brute.X = vale.X + 90;
            brute.Y = vale.Y;
            var before = vale.X;
            ArenaMatch.Step(0.05);
            Assert.Greater(vale.X, before, "pinned Vale did not dash inward");
            Assert.Greater(ArenaLogic.Dashes + ArenaLogic.EscapeDashes, 0, "no corner dash was counted");
        }

    }
}
