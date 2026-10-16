using System.IO;
using BattleTv.Fight;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BattleTv.Menu.Tests
{
    public sealed class MenuTests
    {
        const int PlateBytes = 282368;
        const int ClipBytes = 2145773;
        const int RestBytes = 3216639;

        [Test]
        public void SixteenByNineFillsThePlate()
        {
            var fit = MenuController.Fit(1920f, 1080f);
            Assert.AreEqual(fit.Window, fit.Drawn);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), fit.Uv);
            AssertFighters(fit);
        }

        [Test]
        public void TwentyOneByNineCropsTopAndBottomEqually()
        {
            var fit = MenuController.Fit(2520f, 1080f);
            Assert.AreEqual(0f, fit.Uv.x, 0.0001f);
            Assert.AreEqual(1f, fit.Uv.width, 0.0001f);
            Assert.Less(fit.Uv.height, 1f);
            float sideCrop = (1f - fit.Uv.height) * 0.5f;
            Assert.AreEqual(sideCrop, fit.Uv.y, 0.0001f);
            Assert.AreEqual(2520f, fit.Drawn.width, 0.01f);
            Assert.Greater(fit.Drawn.height, fit.Window.height);
            AssertFighters(fit);
        }

        [Test]
        public void FourByThreeLetterboxes()
        {
            var fit = MenuController.Fit(1600f, 1200f);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), fit.Uv);
            Assert.AreEqual(1600f, fit.Drawn.width, 0.01f);
            Assert.Less(fit.Drawn.height, fit.Window.height);
            float bar = (fit.Window.height - fit.Drawn.height) * 0.5f;
            Assert.AreEqual(bar, fit.Drawn.y, 0.01f);
            AssertFighters(fit);
        }

        [Test]
        public void PlateAndClipKeepTheirBytes()
        {
            string jpg = Path.Combine(Application.dataPath, "Art", "Menu", "Vale-vs-Brute.jpg");
            string mp4 = Path.Combine(Application.dataPath, "StreamingAssets", "menu", "Vale-vs-Brute-push.mp4");
            string rest = Path.Combine(Application.dataPath, "StreamingAssets", "menu", "Vale-vs-Brute-rest.mp4");
            Assert.AreEqual(PlateBytes, new FileInfo(jpg).Length, jpg);
            Assert.AreEqual(ClipBytes, new FileInfo(mp4).Length, mp4);
            Assert.AreEqual(RestBytes, new FileInfo(rest).Length, rest);
        }

        [Test]
        public void MenuSceneHasOneBattleButton()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Menu/Menu.unity", OpenSceneMode.Single);
            Assert.AreEqual("Menu", scene.name);

            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, buttons.Length);
            var label = buttons[0].GetComponentInChildren<Text>(true);
            Assert.NotNull(label);
            Assert.AreEqual("BATTLE TV", label.text);
            Assert.AreEqual(nameof(IntroGate.EnterMenu), buttons[0].onClick.GetPersistentMethodName(0));
            Assert.AreEqual(1, buttons[0].onClick.GetPersistentEventCount());

            var selectables = Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, selectables.Length);

            var still = GameObject.Find("Still").GetComponent<Image>();
            Assert.IsFalse(still.preserveAspect);
            Assert.AreEqual("Vale-vs-Brute", still.sprite.name);
            AssertLoop("Rest", looping: true);
            AssertLoop("Clip", looping: false);
            Assert.AreEqual("intro", Object.FindAnyObjectByType<PageChrome>().Screen);

            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(3, scenes.Length);
            Assert.AreEqual("Assets/Menu/Menu.unity", scenes[0].path);
            Assert.AreEqual("Assets/Menu/GameMenu.unity", scenes[1].path);
            Assert.AreEqual("Assets/Fight/Duel.unity", scenes[2].path);
            Assert.IsTrue(scenes[0].enabled);
            Assert.IsTrue(scenes[1].enabled);
            Assert.IsTrue(scenes[2].enabled);
        }

        [Test]
        public void GameMenuLoopsTheRestPlate()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Menu/GameMenu.unity", OpenSceneMode.Single);
            Assert.AreEqual("GameMenu", scene.name);
            Assert.IsNull(GameObject.Find("Clip"));
            AssertLoop("Rest", looping: true);
            Assert.AreEqual("menu", Object.FindAnyObjectByType<PageChrome>().Screen);
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(5, buttons.Length);
        }

        static void AssertLoop(string objectName, bool looping)
        {
            var player = GameObject.Find(objectName).GetComponent<VideoPlayer>();
            Assert.NotNull(player);
            Assert.AreEqual(looping, player.isLooping);
            Assert.AreEqual(VideoAudioOutputMode.None, player.audioOutputMode);
            Assert.IsFalse(player.GetComponent<RawImage>().raycastTarget);
        }

        [Test]
        public void PageChromeNamesTheStudio()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "page-chrome.html"));
            string html = File.ReadAllText(path);
            StringAssert.Contains("Created by Justin Helmer", html);
            StringAssert.Contains("https://chatagent.ca/games/lattice-arena/", html);
            StringAssert.Contains("https://www.paypal.com/paypalme/ExcavationPro", html);
            StringAssert.Contains("https://www.patreon.com/Excavationpro/posts/chatagent-ca-api-170485961", html);
            StringAssert.Contains(">Options<", html);
        }

        [Test]
        public void ValeAndBruteKeepTheRosterBuild()
        {
            var vale = SideDuel.Vale();
            var brute = SideDuel.Brute();
            Assert.AreEqual(1056, vale.Max);
            Assert.AreEqual(1256, brute.Max);
            Assert.AreEqual(12, vale.Armor);
            Assert.AreEqual(9, brute.Armor);
            Assert.AreEqual(14, SideDuel.Hit(vale, brute, vale.Dmg, true, false, false));
            Assert.AreEqual(0, SideDuel.Hit(brute, vale, 5, true, false, false));
            Assert.AreEqual(60, vale.Mana);
            Assert.AreEqual(40, brute.Mana);
            double plate = vale.Shield;
            SideDuel.RaiseWall(vale);
            plate = vale.Shield;
            SideDuel.RaiseWall(vale);
            Assert.AreEqual(plate, vale.Shield);
            Assert.AreEqual(27, vale.Mana);

            var fresh = SideDuel.Vale();
            var wall = SideDuel.Brute();
            Assert.IsFalse(SideDuel.TryClose(fresh, wall));
            wall.Range = 8;
            fresh.X = 1;
            wall.X = 9;
            Assert.IsTrue(SideDuel.TryClose(fresh, wall));
            Assert.AreEqual(30, fresh.StrideCd);
            Assert.AreEqual(52, fresh.Mana);

            var bruteHome = SideDuel.Brute();
            var valeFar = SideDuel.Vale();
            bruteHome.X = 8;
            valeFar.X = 12;
            Assert.IsTrue(SideDuel.TryClose(bruteHome, valeFar));
            Assert.AreEqual(30, bruteHome.StrideCd);
            Assert.AreEqual(32, bruteHome.Mana);
        }

        [Test]
        public void ValeAndBruteFinishALane()
        {
            int valeWins = 0;
            int bruteWins = 0;
            int draws = 0;
            double longest = 0;
            for (int seed = 0; seed < 12; seed++)
            {
                SideDuel.ResetLane();
                var vale = SideDuel.Vale();
                var brute = SideDuel.Brute();
                var rng = new System.Random(seed);
                double t = 0;
                while (t < SideDuel.HardStop && vale.Alive && brute.Alive)
                {
                    SideDuel.Step(vale, brute, 1.0 / 30.0, rng, t);
                    t += 1.0 / 30.0;
                }
                if (t > longest)
                    longest = t;
                if (vale.Alive && brute.Alive)
                    draws++;
                else if (vale.Alive)
                    valeWins++;
                else
                    bruteWins++;
            }
            Assert.AreEqual(0, draws);
            Assert.Less(longest, 280);
            Assert.Greater(valeWins, 0);
            Assert.Greater(bruteWins, 0);
        }

        static void AssertFighters(MenuController.FitResult fit)
        {
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.LeftFighterThird));
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.RightFighterThird));
        }
    }
}
