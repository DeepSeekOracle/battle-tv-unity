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
                Assert.IsNotNull(body.Find("walk"), id + " walk clip missing");
            }
        }
    }
}
