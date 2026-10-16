using System.Collections.Generic;
using BattleTv.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Fight
{
    /// <summary>
    /// Overhead sand. The plate is the night colosseum. Vale and Brute use the
    /// painted bodies. Every other kit is a mark until that character is built.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        // The white ring on pit.jpg (1920×1080). The fight sits inside it.
        public const float SandLeft = 0.22f;
        public const float SandRight = 0.78f;
        public const float SandTop = 0.48f;
        public const float SandBottom = 0.86f;
        const float StageWidth = 1920f;
        const float StageHeight = 1080f;

        static readonly Color Gold = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
        static readonly Color Paper = new Color(0xf6 / 255f, 0xe7 / 255f, 0xc1 / 255f, 1f);

        [SerializeField] RectTransform plate;
        [SerializeField] Sprite valeStill;
        [SerializeField] Sprite bruteStill;
        [SerializeField] Sprite mark;
        [SerializeField] Sprite barFrame;
        [SerializeField] Sprite pipFrame;
        [SerializeField] Sprite buttonSkin;
        [SerializeField] Sprite fillHealth;
        [SerializeField] Sprite fillMana;
        [SerializeField] Sprite fillStam;
        [SerializeField] Sprite fillBlock;
        [SerializeField] Sprite fillDash;

        Font _font;
        readonly List<Actor> _actors = new List<Actor>(10);
        readonly List<Actor> _bolts = new List<Actor>(16);
        Text _clock;
        Text _callout;
        GameObject _board;
        Text _boardTitle;
        Text _boardDetail;
        bool _boardUp;
        int _appliedW = -1;
        int _appliedH = -1;
        AudioSource _voice;
        readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>(8);

        sealed class Actor
        {
            public ArenaBody Body;
            public ArenaBolt Bolt;
            public RectTransform Rect;
            public Image Image;
            public Image Shadow;
            public Text Name;
            public LaneHud Hud;
            public double Heard;
        }

        void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (IntroMusic.Current != null)
                IntroMusic.Current.SetMuted(true);
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            if (ShowPick.Mode > 1 && ShowPick.Yours.Count > 0 && ShowPick.Foes.Count == ShowPick.Yours.Count)
                ArenaMatch.Begin(ShowPick.Yours, ShowPick.Foes, UnityEngine.Random.Range(1, 9999));
            else
                ArenaMatch.Begin(new[] { "vale", "kael" }, new[] { "brute", "gate" }, 1);
            BuildChrome();
            Spawn();
            Play("fight", 0.45f);
        }

        void Update()
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Fit();
            if (!_boardUp)
            {
                var before = AliveCount();
                ArenaMatch.Step(dt);
                if (AliveCount() < before)
                    Play("kill", 0.5f);
            }
            Paint();
            if (ArenaMatch.Over && !_boardUp)
                ShowBoard();
        }

        void Spawn()
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                var actor = new Actor { Body = body };
                var node = new GameObject(body.Id, typeof(RectTransform), typeof(Image));
                node.transform.SetParent(plate, false);
                actor.Rect = node.GetComponent<RectTransform>();
                actor.Image = node.GetComponent<Image>();
                actor.Image.raycastTarget = false;
                actor.Image.preserveAspect = true;
                var painted = Still(body.Id);
                if (painted != null)
                {
                    actor.Image.sprite = painted;
                    actor.Image.color = Color.white;
                }
                else
                {
                    actor.Image.sprite = mark;
                    actor.Image.color = Hex(LaneRoster.ById(body.Id).Color);
                }
                var shadow = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadow.transform.SetParent(plate, false);
                actor.Shadow = shadow.GetComponent<Image>();
                actor.Shadow.sprite = mark;
                actor.Shadow.color = new Color(0f, 0f, 0f, 0.38f);
                actor.Shadow.raycastTarget = false;
                var label = Words(plate, body.Name);
                label.fontSize = 14;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Paper;
                actor.Name = label;
                actor.Hud = Column(TeamIndex(body), body.Team == 0);
                _actors.Add(actor);
            }
        }

        static int TeamIndex(ArenaBody body)
        {
            var n = 0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                if (ArenaMatch.Bodies[i] == body)
                    return n;
                if (ArenaMatch.Bodies[i].Team == body.Team)
                    n++;
            }
            return n;
        }

        void Paint()
        {
            if (plate == null)
                return;
            var width = plate.rect.width;
            var height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            _actors.Sort((a, b) => a.Body.Y.CompareTo(b.Body.Y));
            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                var body = actor.Body;
                var point = OnSand(body.X, body.Y, width, height);
                var depth = (float)(body.Y / ArenaMatch.WorldH);
                var scale = Mathf.Lerp(0.62f, 1.05f, depth);
                var spriteH = height * 0.15f * scale;
                var aspect = 0.72f;
                if (actor.Image.sprite != null && actor.Image.sprite.rect.height > 1f)
                    aspect = actor.Image.sprite.rect.width / actor.Image.sprite.rect.height;
                actor.Rect.anchorMin = actor.Rect.anchorMax = new Vector2(0.5f, 0.5f);
                actor.Rect.pivot = new Vector2(PivotX(body.Id, actor.Image), 0f);
                actor.Rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
                actor.Rect.anchoredPosition = point;
                var foe = NearestX(body);
                var face = 1f;
                if (body.Id == "vale" || (body.Id != "brute" && Still(body.Id) == null))
                    face = foe >= body.X ? 1f : -1f;
                else if (body.Id == "brute")
                    face = foe >= body.X ? -1f : 1f;
                if (!body.Alive)
                    face = 1f;
                actor.Rect.localScale = new Vector3(face, 1f, 1f);
                if (body.Swing > 0.22 && actor.Heard < 0.05)
                    Play("hit", 0.22f);
                actor.Heard = body.Swing;
                actor.Image.color = body.Alive
                    ? (Still(body.Id) != null ? Color.white : Hex(LaneRoster.ById(body.Id).Color))
                    : new Color(0.35f, 0.28f, 0.22f, 0.8f);
                actor.Rect.SetSiblingIndex(i + 2);
                var shadow = actor.Shadow.rectTransform;
                shadow.anchorMin = shadow.anchorMax = new Vector2(0.5f, 0.5f);
                shadow.pivot = new Vector2(0.5f, 0.5f);
                shadow.sizeDelta = new Vector2(spriteH * 0.55f, spriteH * 0.16f);
                shadow.anchoredPosition = point + new Vector2(0f, spriteH * 0.02f);
                shadow.SetSiblingIndex(1);
                var name = actor.Name.rectTransform;
                name.anchorMin = name.anchorMax = new Vector2(0.5f, 0.5f);
                name.pivot = new Vector2(0.5f, 0f);
                name.sizeDelta = new Vector2(160f, 18f);
                name.anchoredPosition = point + new Vector2(0f, spriteH + 2f);
                if (actor.Hud != null)
                {
                    var held = body.StrideCd <= 0.05 && body.Mana < SideDuel.CloseCost;
                    actor.Hud.ShowPools(body.Name, body.Cards, body.Hp, body.Max, body.Mana, body.ManaMax, body.AtkCd, body.Rate, body.Shield, body.StrideCd, held);
                }
            }
            PaintBolts(width, height);
            if (_clock != null)
            {
                var secs = Mathf.FloorToInt((float)ArenaMatch.Time);
                var face = (secs / 60) + ":" + (secs % 60).ToString("00");
                if (ArenaMatch.Time >= SideDuel.SuddenAt)
                {
                    var pct = (int)System.Math.Round((SideDuel.SuddenScale(ArenaMatch.Time) - 1) * 100);
                    face += "  +" + pct;
                }
                _clock.text = face;
                _clock.color = ArenaMatch.Time >= SideDuel.SuddenAt ? Gold : Paper;
            }
            if (_callout != null && !ArenaMatch.Over)
            {
                var line = "";
                for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                {
                    var body = ArenaMatch.Bodies[i];
                    if (body.Alive && body.Swing > 0.02 && !string.IsNullOrEmpty(body.Callout))
                        line = body.Name + "  " + body.Callout;
                }
                if (line.Length > 0)
                    _callout.text = line;
            }
        }

        void PaintBolts(float width, float height)
        {
            while (_bolts.Count < ArenaMatch.Bolts.Count)
            {
                var node = new GameObject("Bolt", typeof(RectTransform), typeof(Image));
                node.transform.SetParent(plate, false);
                var image = node.GetComponent<Image>();
                image.sprite = mark;
                image.color = Gold;
                image.raycastTarget = false;
                _bolts.Add(new Actor { Rect = node.GetComponent<RectTransform>(), Image = image });
            }
            for (var i = 0; i < _bolts.Count; i++)
            {
                var show = i < ArenaMatch.Bolts.Count;
                _bolts[i].Image.enabled = show;
                if (!show)
                    continue;
                var bolt = ArenaMatch.Bolts[i];
                var rect = _bolts[i].Rect;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(14f, 14f);
                rect.anchoredPosition = OnSand(bolt.X, bolt.Y, width, height);
                rect.SetAsLastSibling();
            }
        }

        Vector2 OnSand(double x, double y, float width, float height)
        {
            var u = SandLeft + (float)(x / ArenaMatch.WorldW) * (SandRight - SandLeft);
            var v = SandTop + (float)(y / ArenaMatch.WorldH) * (SandBottom - SandTop);
            return new Vector2((u - 0.5f) * width, (0.5f - v) * height);
        }

        static float PivotX(string id, Image image)
        {
            if (id == "brute" && image != null && image.sprite != null && image.sprite.rect.width > 1f)
                return image.sprite.pivot.x / image.sprite.rect.width;
            return 0.5f;
        }

        static double NearestX(ArenaBody body)
        {
            var best = body.Team == 0 ? body.X + 10 : body.X - 10;
            var bestD = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var other = ArenaMatch.Bodies[i];
                if (!other.Alive || other.Team == body.Team)
                    continue;
                var d = (other.X - body.X) * (other.X - body.X) + (other.Y - body.Y) * (other.Y - body.Y);
                if (d < bestD)
                {
                    bestD = d;
                    best = other.X;
                }
            }
            return best;
        }

        Sprite Still(string id)
        {
            if (id == "vale")
                return valeStill;
            if (id == "brute")
                return bruteStill;
            return null;
        }

        void BuildChrome()
        {
            _clock = Words(transform, "0:00");
            var clock = _clock.rectTransform;
            clock.anchorMin = new Vector2(0.40f, 0.90f);
            clock.anchorMax = new Vector2(0.60f, 0.98f);
            clock.offsetMin = clock.offsetMax = Vector2.zero;
            _clock.alignment = TextAnchor.MiddleCenter;
            _clock.fontSize = 22;
            _callout = Words(transform, "Colosseum  ·  swept sand");
            var call = _callout.rectTransform;
            call.anchorMin = new Vector2(0.30f, 0.845f);
            call.anchorMax = new Vector2(0.70f, 0.90f);
            call.offsetMin = call.offsetMax = Vector2.zero;
            _callout.alignment = TextAnchor.MiddleCenter;
            _callout.fontSize = 16;
            _callout.color = Gold;
            var menu = Button(transform, "MENU", new Vector2(0.016f, 0.018f), new Vector2(0.12f, 0.072f));
            menu.onClick.AddListener(BackToMenu);
            _board = new GameObject("Scoreboard", typeof(RectTransform), typeof(Image));
            _board.transform.SetParent(transform, false);
            var board = _board.GetComponent<RectTransform>();
            board.anchorMin = new Vector2(0.28f, 0.28f);
            board.anchorMax = new Vector2(0.72f, 0.72f);
            board.offsetMin = board.offsetMax = Vector2.zero;
            _board.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.92f);
            _boardTitle = Words(_board.transform, "");
            var title = _boardTitle.rectTransform;
            title.anchorMin = new Vector2(0.06f, 0.62f);
            title.anchorMax = new Vector2(0.94f, 0.90f);
            title.offsetMin = title.offsetMax = Vector2.zero;
            _boardTitle.alignment = TextAnchor.MiddleCenter;
            _boardTitle.fontSize = 28;
            _boardDetail = Words(_board.transform, "");
            var detail = _boardDetail.rectTransform;
            detail.anchorMin = new Vector2(0.08f, 0.40f);
            detail.anchorMax = new Vector2(0.92f, 0.62f);
            detail.offsetMin = detail.offsetMax = Vector2.zero;
            _boardDetail.alignment = TextAnchor.MiddleCenter;
            _boardDetail.fontSize = 16;
            _boardDetail.color = Gold;
            var replay = Button(_board.transform, "REPLAY", new Vector2(0.08f, 0.10f), new Vector2(0.46f, 0.32f));
            var toons = Button(_board.transform, "NEW TOONS", new Vector2(0.54f, 0.10f), new Vector2(0.92f, 0.32f));
            replay.onClick.AddListener(() => SceneManager.LoadScene("Arena"));
            toons.onClick.AddListener(() =>
            {
                if (IntroMusic.Current != null)
                    IntroMusic.Current.SetMuted(false);
                SceneManager.LoadScene("TeamSelect");
            });
            _board.SetActive(false);
        }

        LaneHud Column(int index, bool left)
        {
            var row = index % 5;
            var y1 = 0.88f - row * 0.15f;
            var y0 = y1 - 0.14f;
            var min = new Vector2(left ? 0.012f : 0.77f, y0);
            var max = new Vector2(left ? 0.23f : 0.988f, y1);
            return LaneHud.Create(transform, (left ? "L" : "R") + row, min, max, !left,
                barFrame, pipFrame, fillHealth, fillMana, fillStam, fillBlock, fillDash);
        }

        void ShowBoard()
        {
            _boardUp = true;
            if (_boardTitle != null)
                _boardTitle.text = ArenaMatch.Title;
            if (_boardDetail != null)
                _boardDetail.text = ArenaMatch.Detail;
            if (_board != null)
                _board.SetActive(true);
            if (ArenaMatch.Title.IndexOf("takes", System.StringComparison.Ordinal) >= 0)
                Play("win", 0.55f);
        }

        public void BackToMenu()
        {
            ShowPick.LeaveToMode();
            if (IntroMusic.Current != null)
                IntroMusic.Current.SetMuted(false);
            SceneManager.LoadScene("GameMenu");
        }

        void Fit()
        {
            if (plate == null)
                return;
            var w = Screen.width;
            var h = Screen.height;
            if (w < 1 || h < 1 || (w == _appliedW && h == _appliedH))
                return;
            var aspect = StageWidth / StageHeight;
            var drawnW = (float)w;
            var drawnH = w / aspect;
            if (drawnH < h)
            {
                drawnH = h;
                drawnW = h * aspect;
            }
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
            plate.pivot = new Vector2(0.5f, 0.5f);
            plate.sizeDelta = new Vector2(drawnW, drawnH);
            plate.anchoredPosition = Vector2.zero;
            _appliedW = w;
            _appliedH = h;
        }

        int AliveCount()
        {
            var n = 0;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                if (ArenaMatch.Bodies[i].Alive)
                    n++;
            }
            return n;
        }

        void Play(string name, float gain)
        {
            if (_voice == null)
                return;
            if (!_sfx.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name);
                _sfx[name] = clip;
            }
            if (clip != null)
                _voice.PlayOneShot(clip, gain);
        }

        Text Words(Transform parent, string text)
        {
            var node = new GameObject("T", typeof(RectTransform), typeof(Text));
            node.transform.SetParent(parent, false);
            var label = node.GetComponent<Text>();
            label.font = _font;
            label.fontStyle = FontStyle.Bold;
            label.color = Paper;
            label.text = text;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        Button Button(Transform parent, string text, Vector2 min, Vector2 max)
        {
            var node = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var face = node.GetComponent<Image>();
            if (buttonSkin != null)
            {
                face.sprite = buttonSkin;
                face.type = Image.Type.Sliced;
                face.color = Color.white;
            }
            else
                face.color = new Color(0.09f, 0.07f, 0.05f, 0.92f);
            var button = node.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.82f, 1f);
            colors.pressedColor = new Color(0.75f, 0.68f, 0.55f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            var label = Words(node.transform, text);
            Stretch(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Gold;
            label.fontSize = 16;
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Color Hex(string html)
        {
            if (string.IsNullOrEmpty(html))
                return Paper;
            if (html[0] != '#')
                html = "#" + html;
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Paper;
        }
    }
}
