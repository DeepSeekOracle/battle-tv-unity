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
            var select = TeamSelectController.RosterIds("champ");
            Assert.AreEqual(13, select.Length);
            Assert.AreEqual("vale", select[0]);
            Assert.AreEqual("seidon", select[select.Length - 2]);
            Assert.AreEqual("sancora", select[select.Length - 1]);
            var beasts = TeamSelectController.RosterIds("beast");
            Assert.AreEqual("brute", beasts[0]);
            Assert.AreEqual("wraith", beasts[1]);
            Assert.AreEqual("imp", beasts[2]);
            Assert.AreEqual("hurler", beasts[3]);
            Assert.AreEqual("shade", beasts[4]);
            Assert.AreEqual("thief", beasts[5]);
            Assert.AreEqual("drain", beasts[6]);
            Assert.AreEqual("gate", beasts[7]);
            Assert.AreEqual("crown", beasts[8]);
            Assert.AreEqual("smith", beasts[9]);
            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("sancora"));
            Assert.IsNull(ShowPick.BlockReason());
            ShowPick.Clear();
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
            Assert.IsTrue(ShowPick.Toggle("gate"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(3), out block), block);
            Assert.AreEqual(1, ShowPick.Foes.Count);
            Assert.AreNotEqual("gate", ShowPick.Foes[0]);

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
        public void UnpaintedKitIsSettledWithoutAFolder()
        {
            Assert.IsTrue(LaneRoster.InLane("bear"));
            Assert.IsFalse(LaneRoster.Painted("bear"));
            Assert.IsTrue(FighterBodies.Settled("bear"));
            Assert.IsTrue(LaneRoster.Painted("smith"));
            Assert.IsTrue(LaneRoster.Painted("wolf"));
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
        public void AutoShowRotatesPaintedCards()
        {
            var roster = LaneRoster.LaneIds();
            Assert.AreEqual(24, roster.Length);
            Assert.AreEqual("vale", roster[0]);
            Assert.AreEqual("brute", roster[1]);
            Assert.AreEqual("wolf", roster[roster.Length - 1]);
            Assert.IsTrue(LaneRoster.InLane("gate"));
            Assert.IsTrue(LaneRoster.Painted("gate"));
            Assert.IsTrue(LaneRoster.InLane("crown"));
            Assert.IsTrue(LaneRoster.Painted("crown"));
            Assert.IsTrue(LaneRoster.Painted("d9ra"));
            Assert.IsTrue(LaneRoster.Painted("srath"));
            Assert.IsTrue(LaneRoster.Painted("kairos"));
            Assert.IsTrue(LaneRoster.Painted("justicae"));
            Assert.IsTrue(LaneRoster.Painted("seidon"));
            Assert.IsTrue(LaneRoster.Painted("sancora"));
            Assert.IsTrue(LaneRoster.InLane("wraith"));
            Assert.IsTrue(LaneRoster.Painted("wraith"));
            Assert.IsTrue(LaneRoster.InLane("imp"));
            Assert.IsTrue(LaneRoster.Painted("imp"));
            Assert.IsTrue(LaneRoster.InLane("hurler"));
            Assert.IsTrue(LaneRoster.Painted("hurler"));
            Assert.IsTrue(LaneRoster.InLane("shade"));
            Assert.IsTrue(LaneRoster.Painted("shade"));
            Assert.IsTrue(LaneRoster.InLane("thief"));
            Assert.IsTrue(LaneRoster.Painted("thief"));
            Assert.IsTrue(LaneRoster.InLane("drain"));
            Assert.IsTrue(LaneRoster.Painted("drain"));
            Assert.IsTrue(LaneRoster.Painted("gate"));
            Assert.IsTrue(LaneRoster.InLane("crown"));
            Assert.IsTrue(LaneRoster.Painted("crown"));
            Assert.IsTrue(LaneRoster.InLane("smith"));
            Assert.IsTrue(LaneRoster.Painted("smith"));

            Assert.IsTrue(ShowPick.BeginAutonomous(new System.Random(5)));
            Assert.AreEqual("vale", ShowPick.Yours[0]);
            Assert.AreEqual("brute", ShowPick.Foes[0]);
            ShowPick.RememberResult("vale");
            Assert.AreEqual(1, ShowPick.Reign);
            Assert.IsTrue(ShowPick.AdvanceCard());
            Assert.AreEqual("vale", ShowPick.Yours[0]);
            Assert.AreEqual("lightfather", ShowPick.Foes[0]);
            Assert.IsFalse(ShowPick.PaintsValeAndBrute());

            var seen = new System.Collections.Generic.HashSet<string>();
            seen.Add("vale");
            seen.Add("lightfather");
            var guard = 0;
            while (seen.Count < roster.Length && guard < 30)
            {
                var champ = ShowPick.Yours[0];
                ShowPick.RememberResult(champ);
                Assert.IsTrue(ShowPick.AdvanceCard());
                Assert.AreEqual(champ, ShowPick.Yours[0]);
                Assert.IsTrue(LaneRoster.InLane(ShowPick.Foes[0]));
                Assert.AreNotEqual(champ, ShowPick.Foes[0]);
                seen.Add(ShowPick.Foes[0]);
                guard++;
            }
            Assert.AreEqual(roster.Length, seen.Count);

            ShowPick.RememberResult(null);
            Assert.IsTrue(ShowPick.AdvanceCard());
            Assert.IsTrue(LaneRoster.InLane(ShowPick.Yours[0]));
            Assert.IsTrue(LaneRoster.InLane(ShowPick.Foes[0]));
            Assert.AreNotEqual(ShowPick.Yours[0], ShowPick.Foes[0]);
            ShowPick.StopShow();
            Assert.IsFalse(ShowPick.AdvanceCard());
        }

        [Test]
        public void PaintedLaneFightsFinish()
        {
            var cards = new[]
            {
                "lightfather", "kael",
                "kael", "orin",
                "orin", "nia",
                "nia", "lyra",
                "lyra", "arkos",
                "arkos", "vale",
                "brute", "lightfather",
                "vale", "nia",
                "d9ra", "kael",
                "srath", "nia",
                "kairos", "srath",
                "justicae", "kairos",
                "seidon", "justicae",
                "sancora", "seidon",
                "wraith", "brute",
                "imp", "wraith",
                "hurler", "imp",
                "shade", "hurler",
                "thief", "shade",
                "drain", "thief",
                "gate", "drain",
                "crown", "gate",
                "smith", "crown"
            };
            for (var c = 0; c < cards.Length; c += 2)
                FinishLane(cards[c], cards[c + 1], 20 + c);
            ArenaMatch.Begin(new[] { "vale" }, new[] { "brute" }, 1);
            Assert.IsFalse(ArenaMatch.Lane);
        }

        static void FinishLane(string left, string right, int seed)
        {
            ArenaMatch.Begin(new[] { left }, new[] { right }, seed, true);
            Assert.IsTrue(ArenaMatch.Lane, left);
            Assert.AreEqual(left, ArenaMatch.Bodies[0].Id);
            Assert.AreEqual(right, ArenaMatch.Bodies[1].Id);
            var line = ArenaMatch.Bodies[0].Y;
            var start = ArenaMatch.Bodies[0].Hp + ArenaMatch.Bodies[1].Hp;
            var casts = ArenaLogic.Casts;
            var spoke = false;
            var steps = 0;
            while (!ArenaMatch.Over && steps < 13000)
            {
                ArenaMatch.Step(0.05);
                steps++;
                for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                {
                    var body = ArenaMatch.Bodies[i];
                    if (!string.IsNullOrEmpty(body.Callout))
                        spoke = true;
                    Assert.AreEqual(line, body.Y, 0.01, body.Id);
                    Assert.GreaterOrEqual(body.X, ArenaMatch.Inset - 0.01, body.Id);
                    Assert.LessOrEqual(body.X, ArenaMatch.WorldW - ArenaMatch.Inset + 0.01, body.Id);
                }
            }
            Assert.IsTrue(ArenaMatch.Over, left + " vs " + right);
            Assert.Greater(ArenaLogic.Casts, casts, left + " vs " + right);
            Assert.IsTrue(spoke, left + " vs " + right);
            double later = 0;
            var leftUp = false;
            var rightUp = false;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                later += body.Hp;
                if (!body.Alive)
                    continue;
                if (body.Team == 0)
                    leftUp = true;
                else
                    rightUp = true;
            }
            Assert.Less(later, start, left + " vs " + right);
            Assert.IsTrue(leftUp != rightUp || ArenaMatch.Time >= SideDuel.HardStop - 0.1, left + " vs " + right);
        }

        [Test]
        public void PaintedChampionsEnterTheSideLane()
        {
            Assert.AreEqual(2, LaneRoster.LinkedCount);
            Assert.IsFalse(LaneRoster.ById("kael").Linked);
            Assert.IsFalse(LaneRoster.ById("arkos").Linked);
            Assert.IsFalse(LaneRoster.ById("d9ra").Linked);
            Assert.IsFalse(LaneRoster.ById("srath").Linked);
            Assert.IsFalse(LaneRoster.ById("kairos").Linked);
            Assert.IsFalse(LaneRoster.ById("justicae").Linked);
            Assert.IsFalse(LaneRoster.ById("seidon").Linked);
            Assert.IsFalse(LaneRoster.ById("sancora").Linked);
            Assert.IsFalse(LaneRoster.ById("wraith").Linked);
            Assert.IsFalse(LaneRoster.ById("imp").Linked);
            Assert.IsFalse(LaneRoster.ById("hurler").Linked);
            Assert.IsFalse(LaneRoster.ById("shade").Linked);
            Assert.IsFalse(LaneRoster.ById("thief").Linked);
            Assert.IsFalse(LaneRoster.ById("drain").Linked);
            Assert.IsFalse(LaneRoster.ById("gate").Linked);
            Assert.IsFalse(LaneRoster.ById("crown").Linked);
            Assert.IsFalse(LaneRoster.ById("smith").Linked);
            var painted = new[] { "lightfather", "kael", "orin", "nia", "lyra", "arkos", "d9ra", "srath", "kairos", "justicae", "seidon", "sancora", "wraith", "imp", "hurler", "shade", "thief", "drain", "gate", "crown", "smith", "wolf" };
            for (var i = 0; i < painted.Length; i++)
            {
                var id = painted[i];
                Assert.IsTrue(LaneRoster.InLane(id), id);
                ShowPick.Clear();
                ShowPick.BeginDraft(1);
                Assert.IsTrue(ShowPick.Toggle(id), id);
                Assert.IsTrue(ShowPick.TryField(new System.Random(11), out var block), id + " " + block);
                Assert.AreEqual(1, ShowPick.Foes.Count);
                Assert.IsTrue(LaneRoster.InLane(ShowPick.Foes[0]), id);
                Assert.AreNotEqual(id, ShowPick.Foes[0]);
                Assert.IsFalse(ShowPick.PaintsValeAndBrute());
            }
            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("vale"));
            Assert.IsTrue(ShowPick.TryField(new System.Random(1), out var classic));
            Assert.IsNull(classic);
            Assert.AreEqual("brute", ShowPick.Foes[0]);
            Assert.IsTrue(ShowPick.PaintsValeAndBrute());

            ArenaMatch.Begin(new[] { "kael" }, new[] { "lyra" }, 4, true);
            Assert.IsTrue(ArenaMatch.Lane);
            Assert.AreEqual(2, ArenaMatch.Bodies.Count);
            var line = ArenaMatch.Bodies[0].Y;
            Assert.AreEqual(line, ArenaMatch.Bodies[1].Y, 0.01);
            Assert.IsFalse(ArenaMatch.Bodies[0].Gun);
            Assert.IsTrue(ArenaMatch.Bodies[1].Gun);
            for (var step = 0; step < 90; step++)
                ArenaMatch.Step(0.05);
            Assert.AreEqual(line, ArenaMatch.Bodies[0].Y, 0.01);
            Assert.AreEqual(line, ArenaMatch.Bodies[1].Y, 0.01);
            Assert.Greater(System.Math.Abs(ArenaMatch.Bodies[0].X - ArenaMatch.Bodies[1].X), 1);
            ArenaMatch.Begin(new[] { "vale" }, new[] { "brute" }, 1);
            Assert.IsFalse(ArenaMatch.Lane);
            ShowPick.StopShow();
        }

        [Test]
        public void BookKitsFieldEveryMode()
        {
            var modes = new[] { 1, 2, 3, 5 };
            for (var m = 0; m < modes.Length; m++)
            {
                var mode = modes[m];
                for (var i = 0; i < LaneRoster.All.Length; i++)
                {
                    var id = LaneRoster.All[i].Id;
                    ShowPick.Clear();
                    ShowPick.BeginDraft(mode);
                    Assert.IsTrue(ShowPick.Toggle(id), id + " mode " + mode);
                    var n = 1;
                    for (var j = 0; n < mode && j < LaneRoster.All.Length; j++)
                    {
                        if (LaneRoster.All[j].Id == id)
                            continue;
                        Assert.IsTrue(ShowPick.Toggle(LaneRoster.All[j].Id), LaneRoster.All[j].Id);
                        n++;
                    }
                    Assert.AreEqual(mode, ShowPick.Yours.Count, id);
                    Assert.IsTrue(ShowPick.TryField(new System.Random(mode * 100 + i), out var block), id + " " + block);
                    Assert.AreEqual(mode, ShowPick.Foes.Count, id);
                    var lane = mode == 1;
                    ArenaMatch.Begin(ShowPick.Yours, ShowPick.Foes, 3, lane);
                    Assert.AreEqual(mode * 2, ArenaMatch.Bodies.Count, id);
                    Assert.AreEqual(lane, ArenaMatch.Lane, id);
                    for (var b = 0; b < ArenaMatch.Bodies.Count; b++)
                    {
                        var body = ArenaMatch.Bodies[b];
                        Assert.Greater(body.Moves.Count, 0, body.Id);
                        if (body.Id == "wraith" || body.Id == "drain")
                        {
                            var cloud = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                if (body.Moves[mi].Name == "Drain Cloud")
                                    cloud = true;
                            }
                            Assert.IsTrue(cloud, body.Id);
                        }
                        if (body.Id == "wraith")
                        {
                            var slip = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                if (body.Moves[mi].Name == "Slip")
                                    slip = true;
                            }
                            Assert.IsTrue(body.Gun, body.Id);
                            Assert.IsTrue(slip, body.Id);
                        }
                        if (body.Id == "imp")
                        {
                            var fire = false;
                            var flicker = false;
                            var slip = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Imp Fire")
                                    fire = true;
                                if (move.Name == "Flicker")
                                    flicker = true;
                                if (move.Name == "Slip")
                                    slip = true;
                                Assert.AreNotEqual("Coal Blink", move.Name, body.Id);
                                Assert.AreNotEqual("Coal Claw", move.Name, body.Id);
                            }
                            Assert.IsTrue(fire, body.Id);
                            Assert.IsTrue(flicker, body.Id);
                            Assert.IsTrue(body.Gun, body.Id);
                            Assert.IsTrue(slip, body.Id);
                        }
                        if (body.Id == "hurler")
                        {
                            var lob = false;
                            var pound = false;
                            var slip = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Lob Stone")
                                    lob = true;
                                if (move.Name == "Ground Pound")
                                    pound = true;
                                if (move.Name == "Slip")
                                    slip = true;
                            }
                            Assert.IsTrue(lob, body.Id);
                            Assert.IsTrue(pound, body.Id);
                            Assert.IsTrue(body.Gun, body.Id);
                            Assert.IsTrue(slip, body.Id);
                        }
                        if (body.Id == "shade")
                        {
                            var flicker = false;
                            var snatch = false;
                            var close = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Flicker")
                                    flicker = true;
                                if (move.Name == "Snatch")
                                    snatch = true;
                                if (move.Name == "Close")
                                    close = true;
                                Assert.AreNotEqual("Shade Snatch", move.Name, body.Id);
                                Assert.AreNotEqual("Coal Blink", move.Name, body.Id);
                            }
                            Assert.IsTrue(flicker, body.Id);
                            Assert.IsTrue(snatch, body.Id);
                            Assert.IsTrue(close, body.Id);
                            Assert.IsFalse(body.Gun, body.Id);
                        }
                        if (body.Id == "thief")
                        {
                            var flicker = false;
                            var snatch = false;
                            var close = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Flicker")
                                    flicker = true;
                                if (move.Name == "Snatch")
                                    snatch = true;
                                if (move.Name == "Close")
                                    close = true;
                                Assert.AreNotEqual("Tithe Snatch", move.Name, body.Id);
                                Assert.AreNotEqual("Shade Snatch", move.Name, body.Id);
                                Assert.AreNotEqual("Coal Blink", move.Name, body.Id);
                            }
                            Assert.AreEqual("Quick Stab", body.Atk, body.Id);
                            Assert.IsTrue(flicker, body.Id);
                            Assert.IsTrue(snatch, body.Id);
                            Assert.IsTrue(close, body.Id);
                            Assert.IsFalse(body.Gun, body.Id);
                        }
                        if (body.Id == "drain")
                        {
                            var cloud = false;
                            var unmake = false;
                            var step = false;
                            var close = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Drain Cloud")
                                    cloud = true;
                                if (move.Name == "Unmake")
                                    unmake = true;
                                if (move.Name == "Grave Step")
                                    step = true;
                                if (move.Name == "Close")
                                    close = true;
                                Assert.AreNotEqual("Unwriting", move.Name, body.Id);
                            }
                            Assert.AreEqual("Drain Touch", body.Atk, body.Id);
                            Assert.IsTrue(cloud, body.Id);
                            Assert.IsTrue(unmake, body.Id);
                            Assert.IsTrue(step, body.Id);
                            Assert.IsTrue(close, body.Id);
                            Assert.IsFalse(body.Gun, body.Id);
                        }
                        if (body.Id == "gate")
                        {
                            var slam = false;
                            var ward = false;
                            var wards = false;
                            var haul = false;
                            var close = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Gate Slam")
                                    slam = true;
                                if (move.Name == "Gate Ward")
                                    ward = true;
                                if (move.Name == "Call the Wards")
                                    wards = true;
                                if (move.Name == "Gate Haul")
                                    haul = true;
                                if (move.Name == "Close")
                                    close = true;
                                Assert.AreNotEqual("Dash", move.Name, body.Id);
                                Assert.AreNotEqual("Slip", move.Name, body.Id);
                            }
                            Assert.AreEqual("Gate Slam", body.Atk, body.Id);
                            Assert.IsTrue(slam, body.Id);
                            Assert.IsTrue(ward, body.Id);
                            Assert.IsTrue(wards, body.Id);
                            Assert.IsTrue(haul, body.Id);
                            Assert.IsTrue(close, body.Id);
                            Assert.IsFalse(body.Gun, body.Id);
                        }
                        if (body.Id == "crown")
                        {
                            var volley = false;
                            var lob = false;
                            var decree = false;
                            var close = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Regal Volley")
                                    volley = true;
                                if (move.Name == "Lob Stone")
                                    lob = true;
                                if (move.Name == "Decree")
                                    decree = true;
                                if (move.Name == "Close")
                                    close = true;
                                Assert.AreNotEqual("Dash", move.Name, body.Id);
                                Assert.AreNotEqual("Slip", move.Name, body.Id);
                            }
                            Assert.AreEqual("Regal Fist", body.Atk, body.Id);
                            Assert.IsTrue(volley, body.Id);
                            Assert.IsTrue(lob, body.Id);
                            Assert.IsTrue(decree, body.Id);
                            Assert.IsTrue(close, body.Id);
                            Assert.IsFalse(body.Gun, body.Id);
                        }
                        if (body.Id == "smith")
                        {
                            var anvil = false;
                            var cannon = false;
                            var temper = false;
                            var slip = false;
                            for (var mi = 0; mi < body.Moves.Count; mi++)
                            {
                                var move = body.Moves[mi];
                                if (move.Name == "Anvil")
                                    anvil = true;
                                if (move.Name == "Gate Cannon")
                                    cannon = true;
                                if (move.Name == "Temper Work")
                                    temper = true;
                                if (move.Name == "Slip")
                                    slip = true;
                                Assert.AreNotEqual("Dash", move.Name, body.Id);
                                Assert.AreNotEqual("Close", move.Name, body.Id);
                            }
                            Assert.AreEqual("Forge Bolt", body.Atk, body.Id);
                            Assert.IsTrue(anvil, body.Id);
                            Assert.IsTrue(cannon, body.Id);
                            Assert.IsTrue(temper, body.Id);
                            Assert.IsTrue(slip, body.Id);
                            Assert.IsTrue(body.Gun, body.Id);
                        }
                        if (lane)
                            Assert.AreEqual(ArenaMatch.WorldH * 0.5, body.Y, 0.01, body.Id);
                    }
                }
            }
            var pastThePair = false;
            for (var s = 1; s <= 12; s++)
            {
                ShowPick.Clear();
                ShowPick.BeginDraft(2);
                ShowPick.RandomDraw(new System.Random(s));
                Assert.AreEqual(2, ShowPick.Yours.Count);
                if (ShowPick.Yours[0] != "vale" && ShowPick.Yours[0] != "brute")
                    pastThePair = true;
                if (ShowPick.Yours[1] != "vale" && ShowPick.Yours[1] != "brute")
                    pastThePair = true;
            }
            Assert.IsTrue(pastThePair);
            ShowPick.StopShow();
        }

        [Test]
        public void ArenaOnboardingLineNamesBothControls()
        {
            StringAssert.Contains("CLOSE", ArenaView.OnboardingLine);
            StringAssert.Contains("SLIP", ArenaView.OnboardingLine);
            Assert.Greater(ArenaView.OnboardingLine.Length, 10);
        }

        [Test]
        public void ArenaFieldUsesTheBook()
        {
            ShowPick.Clear();
            ShowPick.BeginDraft(1);
            Assert.IsTrue(ShowPick.Toggle("kael"));
            ShowPick.Clear();
            Assert.IsTrue(ShowPick.Toggle("gate"));
            ShowPick.Clear();

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
        public void PaintedRosterDashes()
        {
            var ids = LaneRoster.LaneIds();
            Assert.AreEqual(24, ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                Leap(ids[i], true, false);
                Leap(ids[i], false, false);
                Leap(ids[i], true, true);
                Leap(ids[i], false, true);
                LeapVertical(ids[i], true);
                LeapVertical(ids[i], false);
            }
            Sprint("nia");
            Sprint("arkos");
            Sprint("srath");
            var vale = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.streamingAssetsPath, "fighters", "vale", "entity.json"));
            Assert.IsTrue(vale.Contains("\"Close\""));
            Assert.IsTrue(vale.Contains("vale-bash-f00.png"));
        }

        static void Leap(string id, bool leftWall, bool lane)
        {
            var foeId = id == "vale" ? "brute" : "vale";
            ArenaMatch.Begin(new[] { id }, new[] { foeId }, 4, lane);
            var e = ArenaMatch.Bodies[0];
            var foe = ArenaMatch.Bodies[1];
            var y = ArenaMatch.WorldH * 0.5;
            e.X = leftWall ? ArenaMatch.Inset : ArenaMatch.WorldW - ArenaMatch.Inset;
            e.Y = y;
            foe.X = e.X + (leftWall ? 70 : -70);
            foe.Y = y;
            e.Mana = e.ManaMax;
            foe.Mana = 0;
            var expect = e.Gun ? "Slip" : "Close";
            ArenaMatch.Step(0.05);
            var where = id + (leftWall ? " left " : " right ") + (lane ? "lane" : "sand");
            if (leftWall)
                Assert.Greater(e.X, foe.X, where);
            else
                Assert.Less(e.X, foe.X, where);
            Assert.AreEqual(expect, e.Callout, where);
            Assert.AreEqual(30, e.StrideCd, 0.01, where);
            Assert.AreEqual(e.ManaMax - 8, e.Mana, 0.01, where);
            if (lane)
                Assert.AreEqual(y, e.Y, 0.01, where);
            ArenaMatch.Step(0.05);
            Assert.Greater(e.StrideCd, 20, where);
        }

        static void LeapVertical(string id, bool top)
        {
            var foeId = id == "vale" ? "brute" : "vale";
            ArenaMatch.Begin(new[] { id }, new[] { foeId }, 4, false);
            var e = ArenaMatch.Bodies[0];
            var foe = ArenaMatch.Bodies[1];
            var x = ArenaMatch.WorldW * 0.5;
            e.X = x;
            e.Y = top ? ArenaMatch.Inset : ArenaMatch.WorldH - ArenaMatch.Inset;
            foe.X = x;
            foe.Y = e.Y + (top ? 70 : -70);
            e.Mana = e.ManaMax;
            foe.Mana = 0;
            ArenaMatch.Step(0.05);
            var where = id + (top ? " top" : " bottom");
            if (top)
                Assert.Greater(e.Y, foe.Y, where);
            else
                Assert.Less(e.Y, foe.Y, where);
            Assert.AreEqual(e.Gun ? "Slip" : "Close", e.Callout, where);
            Assert.AreEqual(30, e.StrideCd, 0.01, where);
        }

        static void Sprint(string id)
        {
            ArenaMatch.Begin(new[] { id }, new[] { "brute" }, 6, true);
            var e = ArenaMatch.Bodies[0];
            var foe = ArenaMatch.Bodies[1];
            var y = ArenaMatch.WorldH * 0.5;
            e.X = ArenaMatch.WorldW * 0.5;
            e.Y = y;
            foe.X = e.X + 80;
            foe.Y = y;
            e.Mana = e.ManaMax;
            foe.Mana = 0;
            var found = false;
            for (var i = 0; i < e.Moves.Count; i++)
            {
                if (e.Moves[i].Name == "Dash")
                    found = true;
                else
                    e.Cds[i] = 99;
            }
            Assert.IsTrue(found, id);
            var before = e.X;
            ArenaMatch.Step(0.05);
            Assert.AreEqual("Dash", e.Callout, id);
            Assert.Greater(e.SwiftT, 2, id);
            Assert.AreEqual(before, e.X, 0.01, id);
            Assert.AreEqual(y, e.Y, 0.01, id);
            Assert.AreEqual(e.ManaMax - System.Math.Min(6 * 1.1, 40), e.Mana, 0.01, id);
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
            var wolf = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "d9ra", "entity.json"));
            Assert.IsTrue(wolf.Contains("Wolf Fist"));
            Assert.IsTrue(wolf.Contains("Shockwave"));
            Assert.IsTrue(wolf.Contains("Wolf Fury"));
            Assert.IsTrue(wolf.Contains("Squire's Charge"));
            Assert.IsTrue(wolf.Contains("Close"));
            Assert.IsFalse(wolf.Contains("\"Dash\""));
            Assert.IsFalse(wolf.Contains("Slip"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "d9ra", "d9ra.png")));
            var needle = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "srath", "entity.json"));
            Assert.IsTrue(needle.Contains("\"Needle\""));
            Assert.IsTrue(needle.Contains("Doublespeak"));
            Assert.IsTrue(needle.Contains("Needle Rain"));
            Assert.IsTrue(needle.Contains("\"Dash\""));
            Assert.IsTrue(needle.Contains("Slip"));
            Assert.IsFalse(needle.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "srath", "srath.png")));
            var hour = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "kairos", "entity.json"));
            Assert.IsTrue(hour.Contains("Hour Bolt"));
            Assert.IsTrue(hour.Contains("Right-Time"));
            Assert.IsTrue(hour.Contains("Borrowed Second"));
            Assert.IsTrue(hour.Contains("\u03949 Seal"));
            Assert.IsTrue(hour.Contains("Slip"));
            Assert.IsFalse(hour.Contains("\"Dash\""));
            Assert.IsFalse(hour.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "kairos", "kairos.png")));
            var accord = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "justicae", "entity.json"));
            Assert.IsTrue(accord.Contains("Accord Blade"));
            Assert.IsTrue(accord.Contains("Fair Plate"));
            Assert.IsTrue(accord.Contains("Verdict"));
            Assert.IsTrue(accord.Contains("Bulwark"));
            Assert.IsTrue(accord.Contains("Writ of Pursuit"));
            Assert.IsTrue(accord.Contains("Close"));
            Assert.IsFalse(accord.Contains("\"Dash\""));
            Assert.IsFalse(accord.Contains("Slip"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "justicae", "justicae.png")));
            var tide = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "seidon", "entity.json"));
            Assert.IsTrue(tide.Contains("Tide Bolt"));
            Assert.IsTrue(tide.Contains("Undertow"));
            Assert.IsTrue(tide.Contains("Current"));
            Assert.IsTrue(tide.Contains("Geodesic"));
            Assert.IsTrue(tide.Contains("Slip"));
            Assert.IsFalse(tide.Contains("\"Dash\""));
            Assert.IsFalse(tide.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "seidon", "seidon.png")));
            var flask = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "sancora", "entity.json"));
            Assert.IsTrue(flask.Contains("Chemist Vial"));
            Assert.IsTrue(flask.Contains("Chorus Flask"));
            Assert.IsTrue(flask.Contains("Weave Bloom"));
            Assert.IsTrue(flask.Contains("Cover"));
            Assert.IsTrue(flask.Contains("Slip"));
            Assert.IsFalse(flask.Contains("\"Dash\""));
            Assert.IsFalse(flask.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "sancora", "sancora.png")));
            var shroud = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "wraith", "entity.json"));
            Assert.IsTrue(shroud.Contains("Wraith Bolt"));
            Assert.IsTrue(shroud.Contains("Wail"));
            Assert.IsTrue(shroud.Contains("Drain Cloud"));
            Assert.IsTrue(shroud.Contains("Slip"));
            Assert.IsFalse(shroud.Contains("\"Dash\""));
            Assert.IsFalse(shroud.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "wraith", "wraith.png")));
            var coal = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "imp", "entity.json"));
            Assert.IsTrue(coal.Contains("Ember Spit"));
            Assert.IsTrue(coal.Contains("Imp Fire"));
            Assert.IsTrue(coal.Contains("Flicker"));
            Assert.IsTrue(coal.Contains("Slip"));
            Assert.IsFalse(coal.Contains("\"Dash\""));
            Assert.IsFalse(coal.Contains("Close"));
            Assert.IsFalse(coal.Contains("Coal Blink"));
            Assert.IsFalse(coal.Contains("Coal Claw"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "imp", "imp.png")));
            var stone = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "hurler", "entity.json"));
            Assert.IsTrue(stone.Contains("Stone Toss"));
            Assert.IsTrue(stone.Contains("Lob Stone"));
            Assert.IsTrue(stone.Contains("Ground Pound"));
            Assert.IsTrue(stone.Contains("Slip"));
            Assert.IsFalse(stone.Contains("\"Dash\""));
            Assert.IsFalse(stone.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "hurler", "hurler.png")));
            var veil = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "shade", "entity.json"));
            Assert.IsTrue(veil.Contains("Shade Claw"));
            Assert.IsTrue(veil.Contains("Flicker"));
            Assert.IsTrue(veil.Contains("Snatch"));
            Assert.IsTrue(veil.Contains("Close"));
            Assert.IsFalse(veil.Contains("\"Dash\""));
            Assert.IsFalse(veil.Contains("Slip"));
            Assert.IsFalse(veil.Contains("Shade Snatch"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "shade", "shade.png")));
            var coin = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "thief", "entity.json"));
            Assert.IsTrue(coin.Contains("Quick Stab"));
            Assert.IsTrue(coin.Contains("Flicker"));
            Assert.IsTrue(coin.Contains("Snatch"));
            Assert.IsTrue(coin.Contains("Close"));
            Assert.IsFalse(coin.Contains("\"Dash\""));
            Assert.IsFalse(coin.Contains("Slip"));
            Assert.IsFalse(coin.Contains("Tithe Snatch"));
            Assert.IsFalse(coin.Contains("Shade Snatch"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "thief", "thief.png")));
            var shroud2 = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "drain", "entity.json"));
            Assert.IsTrue(shroud2.Contains("Drain Touch"));
            Assert.IsTrue(shroud2.Contains("Unmake"));
            Assert.IsTrue(shroud2.Contains("Drain Cloud"));
            Assert.IsTrue(shroud2.Contains("Grave Step"));
            Assert.IsTrue(shroud2.Contains("Close"));
            Assert.IsFalse(shroud2.Contains("\"Dash\""));
            Assert.IsFalse(shroud2.Contains("Slip"));
            Assert.IsFalse(shroud2.Contains("Unwriting"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "drain", "drain.png")));
            var door = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "gate", "entity.json"));
            Assert.IsTrue(door.Contains("Gate Slam"));
            Assert.IsTrue(door.Contains("Gate Ward"));
            Assert.IsTrue(door.Contains("Call the Wards"));
            Assert.IsTrue(door.Contains("Gate Haul"));
            Assert.IsTrue(door.Contains("Close"));
            Assert.IsFalse(door.Contains("\"Dash\""));
            Assert.IsFalse(door.Contains("Slip"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "gate", "gate.png")));
            var crown = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "crown", "entity.json"));
            Assert.IsTrue(crown.Contains("Regal Fist"));
            Assert.IsTrue(crown.Contains("Regal Volley"));
            Assert.IsTrue(crown.Contains("Lob Stone"));
            Assert.IsTrue(crown.Contains("Decree"));
            Assert.IsTrue(crown.Contains("Close"));
            Assert.IsFalse(crown.Contains("\"Dash\""));
            Assert.IsFalse(crown.Contains("Slip"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "crown", "crown.png")));
            var smith = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "smith", "entity.json"));
            Assert.IsTrue(smith.Contains("Forge Bolt"));
            Assert.IsTrue(smith.Contains("Anvil"));
            Assert.IsTrue(smith.Contains("Gate Cannon"));
            Assert.IsTrue(smith.Contains("Temper Work"));
            Assert.IsTrue(smith.Contains("Slip"));
            Assert.IsFalse(smith.Contains("\"Dash\""));
            Assert.IsFalse(smith.Contains("Close"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "fighters", "smith", "smith.png")));
        }

        static void AssertFighters(MenuController.FitResult fit)
        {
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.LeftFighterThird));
            Assert.IsTrue(MenuController.FighterThirdStaysVisible(fit, MenuController.RightFighterThird));
        }
    }
}

