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
            Assert.AreEqual(5, scenes.Length);
            Assert.AreEqual("Assets/Menu/Menu.unity", scenes[0].path);
            Assert.AreEqual("Assets/Menu/GameMenu.unity", scenes[1].path);
            Assert.AreEqual("Assets/Menu/TeamSelect.unity", scenes[2].path);
            Assert.AreEqual("Assets/Fight/Duel.unity", scenes[3].path);
            Assert.AreEqual("Assets/Fight/Arena.unity", scenes[4].path);
            Assert.IsTrue(scenes[0].enabled);
            Assert.IsTrue(scenes[1].enabled);
            Assert.IsTrue(scenes[2].enabled);
            Assert.IsTrue(scenes[3].enabled);
            Assert.IsTrue(scenes[4].enabled);
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
        public void PageHeadCarriesShareCards()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string head = File.ReadAllText(Path.Combine(root, "tools", "seo", "page-head.html"));
            StringAssert.Contains("property=\"og:title\"", head);
            StringAssert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", head);
            StringAssert.Contains("name=\"googlebot\"", head);
            StringAssert.Contains("https://deepseekoracle.github.io/battle-tv-unity/", head);
            StringAssert.Contains("https://deepseekoracle.github.io/battle-tv-unity/og.jpg", head);
            StringAssert.Contains("application/ld+json", head);
            string robots = File.ReadAllText(Path.Combine(root, "tools", "seo", "robots.txt"));
            StringAssert.Contains("User-agent: Googlebot", robots);
            StringAssert.Contains("Sitemap: https://deepseekoracle.github.io/battle-tv-unity/sitemap.xml", robots);
            string publisher = File.ReadAllText(Path.Combine(root, "Assets", "Menu", "Editor", "WebGlPublish.cs"));
            StringAssert.Contains("InjectSeo", publisher);
            StringAssert.Contains("page-head.html", publisher);
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

            var pinned = SideDuel.Brute();
            var corner = SideDuel.Vale();
            pinned.X = SideDuel.Wall;
            corner.X = 4.2;
            Assert.IsTrue(SideDuel.TryClose(pinned, corner));
            Assert.Greater(pinned.X, corner.X);
            Assert.AreEqual(30, pinned.StrideCd);
            Assert.AreEqual(32, pinned.Mana);

            var valePin = SideDuel.Vale();
            var onHer = SideDuel.Brute();
            valePin.X = SideDuel.Wall;
            onHer.X = 4.2;
            Assert.IsTrue(SideDuel.TryClose(valePin, onHer));
            Assert.Greater(valePin.X, onHer.X);
            Assert.AreEqual(30, valePin.StrideCd);
            Assert.AreEqual(52, valePin.Mana);
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

        [Test]
        public void RosterKeepsTheThirtyThreeKits()
        {
            Assert.AreEqual(33, LaneRoster.All.Length);
            Assert.AreEqual(13, LaneRoster.Of("champ").Length);
            Assert.AreEqual(15, LaneRoster.Of("beast").Length);
            Assert.AreEqual(5, LaneRoster.Of("pet").Length);
            Assert.AreEqual(2, LaneRoster.LinkedCount);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < LaneRoster.All.Length; i++)
            {
                var kit = LaneRoster.All[i];
                Assert.IsTrue(seen.Add(kit.Id), kit.Id);
                Assert.IsFalse(string.IsNullOrEmpty(kit.Name));
                Assert.Greater(kit.Moves.Length, 0);
                Assert.Greater(kit.Cards.Length, 0);
                for (var m = 0; m < kit.Moves.Length; m++)
                    Assert.AreNotEqual(kit.Moves[m], LaneRoster.MoveName(kit.Moves[m]), kit.Id + " " + kit.Moves[m]);
                for (var c = 0; c < kit.Cards.Length; c++)
                    Assert.AreNotEqual(kit.Cards[c], LaneRoster.CardName(kit.Cards[c]), kit.Id + " " + kit.Cards[c]);
            }
            Assert.IsTrue(LaneRoster.ById("vale").Linked);
            Assert.IsTrue(LaneRoster.ById("brute").Linked);
            Assert.IsFalse(LaneRoster.ById("kael").Linked);

            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("vale"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(1), out var block));
            Assert.IsNull(block);
            Assert.AreEqual("brute", ShowPick.Foes[0]);
            Assert.IsFalse(ShowPick.Autonomous);

            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("brute"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(2), out block));
            Assert.AreEqual("vale", ShowPick.Foes[0]);

            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsFalse(ShowPick.Toggle("kael"));
            ShowPick.Yours.Add("kael");
            Assert.IsFalse(ShowPick.TryField(new System.Random(3), out block));
            StringAssert.Contains("not in the lane", block);

            ShowPick.Clear();
            ShowPick.BeginDraft(2);
            Assert.IsTrue(ShowPick.Toggle("vale"));
            Assert.IsTrue(ShowPick.Toggle("brute"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(4), out block));
            Assert.IsNull(block);
            Assert.AreEqual(2, ShowPick.Foes.Count);
            Assert.AreNotEqual("vale", ShowPick.Foes[0]);
            Assert.AreNotEqual("brute", ShowPick.Foes[0]);

            Assert.IsTrue(ShowPick.BeginAutonomous(new System.Random(5)));
            Assert.IsTrue(ShowPick.Autonomous);
            Assert.AreEqual(1, ShowPick.Mode);
            Assert.AreEqual("vale", ShowPick.Yours[0]);
            Assert.AreEqual("brute", ShowPick.Foes[0]);
            ShowPick.RememberResult("vale");
            Assert.AreEqual(1, ShowPick.Reign);
            ShowPick.RememberResult(null);
            Assert.AreEqual(1, ShowPick.Reign);
            ShowPick.RememberResult("vale");
            Assert.AreEqual(2, ShowPick.Reign);
            ShowPick.StopShow();
            Assert.IsFalse(ShowPick.Autonomous);
            Assert.AreEqual(0, ShowPick.Yours.Count);
        }

        [Test]
        public void DraftMenuKeepsTheLineup()
        {
            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("vale"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(7), out _));
            ShowPick.LeaveToMode();
            Assert.IsFalse(ShowPick.Autonomous);
            Assert.AreEqual("vale", ShowPick.Yours[0]);
            Assert.AreEqual(0, ShowPick.Foes.Count);

            ShowPick.Clear();
            Assert.IsTrue(ShowPick.BeginAutonomous(new System.Random(8)));
            ShowPick.LeaveToMode();
            Assert.IsFalse(ShowPick.Autonomous);
            Assert.AreEqual(0, ShowPick.Yours.Count);
            Assert.AreEqual(0, ShowPick.Foes.Count);

            ShowPick.Clear();
            ShowPick.Focus("brute");
            Assert.AreEqual("brute", ShowPick.Inspect);
            Assert.AreEqual("beast", ShowPick.Filter);
            ShowPick.Focus("vale");
            Assert.AreEqual("champ", ShowPick.Filter);
            ShowPick.Focus("nope");
            Assert.AreEqual("vale", ShowPick.Inspect);
            Assert.AreEqual("champ", ShowPick.Filter);
            ShowPick.StopShow();
        }

        [Test]
        public void ArenaFieldUsesTheBook()
        {
            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsFalse(ShowPick.Toggle("kael"));

            ShowPick.Clear();
            ShowPick.BeginDraft(2);
            Assert.IsTrue(ShowPick.Toggle("vale"));
            Assert.IsTrue(ShowPick.Toggle("kael"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(9), out var block));
            Assert.IsNull(block);
            Assert.AreEqual(2, ShowPick.Foes.Count);
            Assert.IsFalse(ShowPick.PaintsValeAndBrute());

            ArenaMatch.Begin(ShowPick.Yours, ShowPick.Foes, 3);
            Assert.AreEqual(4, ArenaMatch.Bodies.Count);
            Assert.AreEqual(4, LaneRoster.ManaRegen("vale"), 0.01);
            Assert.AreEqual(12, LaneRoster.ManaRegen("orin"), 0.01);
            Assert.AreEqual(3, LaneRoster.ManaRegen("brute"), 0.01);
            double hp = 0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                hp += body.Hp;
                Assert.AreEqual(0, body.Shield, 0.01);
                Assert.GreaterOrEqual(body.X, ArenaMatch.Inset - 0.01);
                Assert.LessOrEqual(body.X, ArenaMatch.WorldW - ArenaMatch.Inset + 0.01);
                if (body.Id == "vale")
                {
                    Assert.AreEqual(1056, body.Max, 0.01);
                    Assert.AreEqual(12, body.Armor, 0.01);
                    Assert.AreEqual("BULWARK · THORN · IRON", body.Cards);
                    Assert.Greater(body.Moves.Count, 3);
                }
            }
            for (var step = 0; step < 500; step++)
                ArenaMatch.Step(0.05);
            Assert.Greater(ArenaLogic.Casts, 0);
            double later = 0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                later += ArenaMatch.Bodies[i].Hp;
                Assert.GreaterOrEqual(ArenaMatch.Bodies[i].X, ArenaMatch.Inset - 0.01);
                Assert.LessOrEqual(ArenaMatch.Bodies[i].X, ArenaMatch.WorldW - ArenaMatch.Inset + 0.01);
                Assert.GreaterOrEqual(ArenaMatch.Bodies[i].Y, ArenaMatch.Inset - 0.01);
                Assert.LessOrEqual(ArenaMatch.Bodies[i].Y, ArenaMatch.WorldH - ArenaMatch.Inset + 0.01);
            }
            Assert.Less(later, hp);
            ShowPick.StopShow();
        }

        [Test]
        public void CorneredFighterLeapsPast()
        {
            ArenaMatch.Begin(new[] { "vale" }, new[] { "brute" }, 1);
            var vale = ArenaMatch.Bodies[0];
            var brute = ArenaMatch.Bodies[1];
            vale.X = ArenaMatch.Inset;
            vale.Y = ArenaMatch.WorldH * 0.5;
            brute.X = vale.X + 70;
            brute.Y = vale.Y;
            vale.Mana = vale.ManaMax;
            brute.Mana = 0;
            ArenaMatch.Step(0.05);
            Assert.Greater(vale.X, brute.X);
            Assert.AreEqual(30, vale.StrideCd, 0.01);
            Assert.AreEqual("Close", vale.Callout);
            Assert.AreEqual(vale.ManaMax - 8, vale.Mana, 0.01);

            ArenaMatch.Begin(new[] { "brute" }, new[] { "vale" }, 1);
            brute = ArenaMatch.Bodies[0];
            vale = ArenaMatch.Bodies[1];
            brute.X = ArenaMatch.WorldW - ArenaMatch.Inset;
            brute.Y = ArenaMatch.WorldH * 0.5;
            vale.X = brute.X - 70;
            vale.Y = brute.Y;
            brute.Mana = brute.ManaMax;
            vale.Mana = 0;
            ArenaMatch.Step(0.05);
            Assert.Less(brute.X, vale.X);
            Assert.AreEqual(30, brute.StrideCd, 0.01);
            Assert.AreEqual("Close", brute.Callout);
        }

        [Test]
        public void FighterFolderKeepsLyraApart()
        {
            var path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "lyra", "entity.json");
            Assert.IsTrue(System.IO.File.Exists(path), path);
            var json = System.IO.File.ReadAllText(path);
            Assert.IsTrue(json.Contains("\"lyra\""));
            Assert.IsTrue(json.Contains("0.72"));
            Assert.IsTrue(json.Contains("Spiral Note"));
            Assert.IsTrue(json.Contains("Weave Bloom"));
            Assert.IsFalse(json.Contains("Spiral Bloom"));
            var note = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "lyra", "lyra-note-f01.png");
            Assert.IsTrue(System.IO.File.Exists(note), note);
            var father = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "lightfather", "entity.json"));
            Assert.IsTrue(father.Contains("\u03949 Seal"));
            var brute = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "brute", "entity.json"));
            Assert.IsTrue(brute.Contains("0.6164"));
            Assert.IsTrue(brute.Contains("brute-drag.png"));
            var arkos = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "arkos", "entity.json"));
            Assert.IsTrue(arkos.Contains("Lance Thrust"));
            Assert.IsTrue(arkos.Contains("Geodesic"));
            Assert.IsTrue(arkos.Contains("True Line"));
            Assert.IsTrue(arkos.Contains("\"Dash\""));
            Assert.IsTrue(arkos.Contains("Close"));
            Assert.IsFalse(arkos.Contains("Slip"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "arkos", "arkos.png")));
        }

        static void AssertFighters(MenuController.FitResult fit)
        {
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.LeftFighterThird));
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.RightFighterThird));
        }
    }
}
