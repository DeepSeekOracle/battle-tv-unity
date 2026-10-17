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
        [SerializeField] Sprite bruteRest;
        [SerializeField] Sprite bruteGrip;
        [SerializeField] Sprite[] valeBash;
        [SerializeField] Sprite[] valePunch;
        [SerializeField] Sprite[] valeToss;
        [SerializeField] Sprite[] valeLine;
        [SerializeField] Sprite[] valePlant;
        [SerializeField] Sprite[] valeDeath;
        [SerializeField] Sprite[] valeWin;
        [SerializeField] Sprite[] bruteSweep;
        [SerializeField] Sprite[] brutePound;
        [SerializeField] Sprite[] bruteWard;
        [SerializeField] Sprite[] bruteClose;
        [SerializeField] Sprite[] bruteDeath;
        [SerializeField] Sprite[] bruteWin;
        [SerializeField] Sprite lightfatherStill;
        [SerializeField] Sprite[] lfBolt;
        [SerializeField] Sprite[] lfSeal;
        [SerializeField] Sprite[] lfProvenance;
        [SerializeField] Sprite[] lfCover;
        [SerializeField] Sprite[] lfStandard;
        [SerializeField] Sprite[] lfSlip;
        [SerializeField] Sprite[] lfDeath;
        [SerializeField] Sprite[] lfWin;
        [SerializeField] Sprite[] valeWalk;
        [SerializeField] Sprite[] bruteWalk;
        [SerializeField] Sprite[] lfWalk;
        [SerializeField] Sprite kaelStill;
        [SerializeField] Sprite[] kaelShard;
        [SerializeField] Sprite[] kaelRend;
        [SerializeField] Sprite[] kaelWall;
        [SerializeField] Sprite[] kaelCharge;
        [SerializeField] Sprite[] kaelClose;
        [SerializeField] Sprite[] kaelDeath;
        [SerializeField] Sprite[] kaelWin;
        [SerializeField] Sprite[] kaelWalk;
        [SerializeField] Sprite orinStill;
        [SerializeField] Sprite[] orinShard;
        [SerializeField] Sprite[] orinUnwrite;
        [SerializeField] Sprite[] orinFlask;
        [SerializeField] Sprite[] orinDrink;
        [SerializeField] Sprite[] orinSlip;
        [SerializeField] Sprite[] orinDeath;
        [SerializeField] Sprite[] orinWin;
        [SerializeField] Sprite[] orinWalk;
        [SerializeField] Sprite niaStill;
        [SerializeField] Sprite[] niaArrow;
        [SerializeField] Sprite[] niaPin;
        [SerializeField] Sprite[] niaDash;
        [SerializeField] Sprite[] niaRain;
        [SerializeField] Sprite[] niaSlip;
        [SerializeField] Sprite[] niaDeath;
        [SerializeField] Sprite[] niaWin;
        [SerializeField] Sprite[] niaWalk;
        [SerializeField] Sprite sigilGlow;
        [SerializeField] Material sigilRingBack;
        [SerializeField] Material sigilRingFront;
        [SerializeField] Material sigilBolt;
        [SerializeField] Material sigilBurst;
        [SerializeField] Material sigilMend;
        [SerializeField] Material sigilEmber;
        [SerializeField] Material sigilLance;

        Font _font;
        readonly List<Actor> _actors = new List<Actor>(10);
        readonly List<Actor> _bolts = new List<Actor>(16);
        readonly List<Sigil> _sigils = new List<Sigil>(48);
        readonly List<Flash> _flashes = new List<Flash>(24);
        int _sigilN;
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
            public Sprite[] Clip;
            public float ClipT = -1f;
            public float ClipDur = 0.4f;
            public float Pivot = 0.5f;
            public bool HoldClip;
            public string Pose = "";
            public double SeenHp = -1;
            public bool SeenAlive = true;
            public float WalkT = -1f;
            public bool HasFoot;
            public double FootX;
            public double FootY;
        }

        sealed class Sigil
        {
            public RectTransform Rect;
            public Image Image;
        }

        sealed class Flash
        {
            public int Kind;
            public double X;
            public double Y;
            public double X2;
            public double Y2;
            public float Life;
            public float Max;
            public float Radius;
            public Color Color;
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
            for (var i = 0; i < _actors.Count; i++)
            {
                AdvanceClip(_actors[i], dt);
                AdvanceWalk(_actors[i], dt);
            }
            EnsureActors();
            Paint();
            AgeFlashes(dt);
            if (ArenaMatch.Over && !_boardUp)
                ShowBoard();
        }

        void Spawn()
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                AddActor(ArenaMatch.Bodies[i]);
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
                actor.Rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
                actor.Rect.anchoredPosition = point;
                var foe = NearestX(body);
                var face = 1f;
                if (body.Id == "brute")
                    face = foe >= body.X ? -1f : 1f;
                else if (body.Id == "vale" || body.Id == "lightfather" || body.Id == "kael" || body.Id == "orin" || body.Id == "nia" || Still(body.Id) == null)
                    face = foe >= body.X ? 1f : -1f;
                if (body.Alive)
                    actor.Rect.localScale = new Vector3(face, 1f, 1f);
                if (body.Swing > 0.22 && actor.Heard < 0.05)
                {
                    var named = body.Callout ?? "";
                    Play(named == "Close" || named == "Slip" ? "cast" : "hit", 0.22f);
                }
                if (body.Alive && body.Swing > actor.Heard + 0.02 && body.Swing > 0.18)
                {
                    Arm(actor, body);
                    NoteCast(body);
                }
                if (!body.Alive && actor.Pose != "death")
                    ArmEnd(actor, body, false);
                if (ArenaMatch.Over && body.Alive && actor.Pose != "win" && actor.Pose != "death")
                    ArmEnd(actor, body, true);
                actor.Heard = body.Swing;
                ShowFrame(actor, body);
                var pivot = (body.Id == "vale" || body.Id == "lightfather" || body.Id == "kael" || body.Id == "orin" || body.Id == "nia") && actor.ClipT >= 0f
                    ? actor.Pivot
                    : PivotX(body.Id, actor.Image);
                actor.Rect.pivot = new Vector2(pivot, 0f);
                var painted = Still(body.Id) != null;
                actor.Image.color = body.Alive
                    ? (painted ? Color.white : Hex(LaneRoster.ById(body.Id).Color))
                    : (painted ? new Color(0.78f, 0.7f, 0.62f, 1f) : new Color(0.35f, 0.28f, 0.22f, 0.8f));
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
            if (sigilBolt != null)
                PaintSigils(width, height);
            else
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

        void NoteCast(ArenaBody body)
        {
            var color = KitColor(body);
            var mv = FindMove(body);
            var shape = mv != null ? mv.Shape : (body.Gun ? "bolt" : "melee");
            var foe = NearestFoe(body);
            var tiles = mv != null && mv.Radius > 0.2 ? mv.Radius : 2.4;
            var radius = (float)(tiles * ArenaMatch.Tile);
            var st = mv != null ? mv.St : null;
            var self = mv != null ? mv.Self : null;
            if (shape == "aura" || shape == "blast")
            {
                var at = shape == "blast" && foe != null ? foe : body;
                Push(3, at.X, at.Y, at.X, at.Y, 0.5f, radius, color);
            }
            else if (shape == "beam" && foe != null)
                Push(4, body.X, body.Y, foe.X, foe.Y, 0.32f, 18f, color);
            else if (shape == "melee" || shape == "arc")
            {
                if (foe != null)
                    Push(5, foe.X, foe.Y, foe.X, foe.Y, 0.22f, 36f, color);
            }
            else if (shape == "bolt" || shape == "multi" || shape == "lob")
                Push(5, body.X, body.Y, body.X, body.Y, 0.14f, 28f, color);
            else if (shape == "revive")
                Push(2, body.X, body.Y, body.X, body.Y, 0.7f, 120f, color);
            else if (shape == "blink" || shape == "stride")
                Push(3, body.X, body.Y, body.X, body.Y, 0.35f, 80f, color);
            else
            {
                var ward = Has(st, self, true);
                var mend = Has(st, self, false);
                Push(ward && !mend ? 3 : 2, body.X, body.Y, body.X, body.Y, 0.45f, ward ? 110f : 80f, color);
            }
        }

        static bool Has(ArenaLogic.Status a, ArenaLogic.Status b, bool ward)
        {
            if (ward)
                return (a != null && (a.Aegis > 0 || a.ThornV > 0 || a.Armor > 0))
                    || (b != null && (b.Aegis > 0 || b.ThornV > 0 || b.Armor > 0));
            return (a != null && (a.Heal > 0 || a.RegenT > 0 || a.HasteD > 0 || a.SwiftD > 0))
                || (b != null && (b.Heal > 0 || b.RegenT > 0 || b.HasteD > 0 || b.SwiftD > 0));
        }

        static ArenaLogic.MoveDef FindMove(ArenaBody body)
        {
            if (body.Moves == null)
                return null;
            var call = body.Callout ?? "";
            for (var i = 0; i < body.Moves.Count; i++)
            {
                if (body.Moves[i].Name == call)
                    return body.Moves[i];
            }
            return null;
        }

        void Push(int kind, double x, double y, double x2, double y2, float life, float radius, Color color)
        {
            _flashes.Add(new Flash
            {
                Kind = kind,
                X = x,
                Y = y,
                X2 = x2,
                Y2 = y2,
                Life = life,
                Max = life,
                Radius = radius,
                Color = color
            });
        }

        void AgeFlashes(float dt)
        {
            for (var i = _flashes.Count - 1; i >= 0; i--)
            {
                _flashes[i].Life -= dt;
                if (_flashes[i].Life <= 0f)
                    _flashes.RemoveAt(i);
            }
        }

        void PaintSigils(float width, float height)
        {
            _sigilN = 0;
            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                var body = actor.Body;
                if (body == null)
                    continue;
                var color = KitColor(body);
                var feet = OnSand(body.X, body.Y, width, height);
                var rise = actor.Rect != null ? actor.Rect.sizeDelta.y * 0.42f : 40f;
                if (!body.Alive && actor.SeenAlive)
                    Push(5, body.X, body.Y, body.X, body.Y, 0.28f, 48f, color);
                if (body.Alive && !actor.SeenAlive)
                    Push(2, body.X, body.Y, body.X, body.Y, 0.65f, 130f, color);
                if (body.Alive && actor.SeenHp >= 0 && body.Hp > actor.SeenHp + 0.75)
                    Push(2, body.X, body.Y, body.X, body.Y, 0.4f, 90f, color);
                if (body.Alive && actor.SeenHp >= 0 && body.Hp < actor.SeenHp - 0.75)
                    Push(5, body.X, body.Y + 18, body.X, body.Y, 0.18f, 34f, color);
                actor.SeenHp = body.Hp;
                actor.SeenAlive = body.Alive;
                if (!body.Alive)
                    continue;
                if (body.Shield > 0.5 || body.ThornsT > 0.05)
                {
                    var ward = body.ThornsT > 0.05 ? 150f : 124f;
                    PlaceRing(feet, ward, color, 0.9f, true);
                }
                if (body.Stun > 0.05 || body.Root > 0.05)
                    PlaceRing(feet + new Vector2(0f, rise), 70f, color, 0.75f, false);
                if (body.RegenT > 0.05 || body.HasteT > 0.05 || body.SwiftT > 0.05 || body.VeilT > 0.05)
                    Place(sigilMend, feet + new Vector2(0f, rise * 0.45f), new Vector2(78f, 36f), 0f, color, 0.55f, false);
                if (body.BurnT > 0.05 || body.BleedT > 0.05)
                    Place(sigilEmber, feet + new Vector2(0f, rise), new Vector2(30f, 30f), 0f, color, 0.8f, false);
            }
            for (var i = 0; i < ArenaMatch.Bolts.Count; i++)
            {
                var bolt = ArenaMatch.Bolts[i];
                var color = bolt.Src != null ? KitColor(bolt.Src) : Gold;
                var at = OnSand(bolt.X, bolt.Y, width, height);
                if (bolt.Lob)
                    Place(sigilEmber, at, new Vector2(26f, 26f), 0f, color, 1f, false);
                else if (bolt.Src != null && bolt.Src.Id == "lightfather")
                    PlaceSealBolt(bolt, at, width, height, color);
                else if (bolt.Src != null && bolt.Src.Id == "orin")
                    PlaceSigilShard(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "nia")
                    PlacePathArrow(bolt, at, width, height);
                else
                {
                    var aim = -(float)bolt.Angle * Mathf.Rad2Deg;
                    Place(sigilBolt, at, new Vector2(46f, 14f), aim, color, 1f, false);
                }
            }
            for (var i = 0; i < _flashes.Count; i++)
            {
                var flash = _flashes[i];
                var k = flash.Life / Mathf.Max(0.05f, flash.Max);
                var grow = flash.Kind == 3 ? Mathf.Lerp(0.45f, 1f, 1f - k) : 1f;
                var at = OnSand(flash.X, flash.Y, width, height);
                var size = WorldSpan(flash.Radius, width) * (flash.Kind == 3 ? 2f : 1f) * grow;
                size = Mathf.Clamp(size, 28f, 520f);
                if (flash.Kind == 4)
                {
                    var to = OnSand(flash.X2, flash.Y2, width, height);
                    var delta = to - at;
                    var mid = (at + to) * 0.5f;
                    var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    Place(sigilLance, mid, new Vector2(delta.magnitude, 16f), ang, flash.Color, k, false);
                }
                else if (flash.Kind == 2)
                    Place(sigilMend, at + new Vector2(0f, size * 0.15f), new Vector2(size, size * 0.42f), 0f, flash.Color, k, false);
                else if (flash.Kind == 5)
                    Place(sigilEmber, at + new Vector2(0f, size * 0.35f), new Vector2(size, size), 0f, flash.Color, k, false);
                else
                    PlaceRing(at, Mathf.Max(64f, size), flash.Color, k, false);
            }
            for (var i = _sigilN; i < _sigils.Count; i++)
                _sigils[i].Image.enabled = false;
        }

        void PlaceSealBolt(ArenaBolt bolt, Vector2 at, float width, float height, Color color)
        {
            // The sim bolt leaves the feet. The picture leaves the triangle in the leading hand.
            var feet = OnSand(bolt.Src.X, bolt.Src.Y, width, height);
            var hand = Triangle(bolt.Src, feet);
            var tip = at + (hand - feet);
            var delta = tip - hand;
            var len = delta.magnitude;
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var mid = (hand + tip) * 0.5f;
            var hot = new Color(1f, 0.97f, 0.78f, 1f);
            if (len > 12f)
            {
                Place(sigilBolt, mid, new Vector2(len, 120f), ang, color, 0.9f, false);
                Place(sigilLance, mid, new Vector2(len, 36f), ang, hot, 1f, false);
            }
            Place(sigilBolt, tip, new Vector2(84f, 52f), ang, hot, 1f, false);
            Place(sigilEmber, tip, new Vector2(40f, 40f), 0f, hot, 1f, false);
            Place(sigilEmber, hand, new Vector2(64f, 64f), 0f, hot, 1f, false);
            var flash = Mathf.Clamp01(1f - len / 280f);
            if (flash > 0.02f)
            {
                var boom = Mathf.Lerp(80f, 220f, flash);
                Place(sigilBurst, hand, new Vector2(boom, boom), 0f, color, 0.5f + 0.45f * flash, false);
                Place(sigilBurst, hand, new Vector2(boom * 0.42f, boom * 0.42f), 0f, hot, flash, false);
            }
        }

        Vector2 Triangle(ArenaBody body, Vector2 feet)
        {
            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Body != body || actor.Rect == null)
                    continue;
                var face = actor.Rect.localScale.x >= 0f ? 1f : -1f;
                var w = actor.Rect.sizeDelta.x;
                var h = actor.Rect.sizeDelta.y;
                // Measured on the 860x1240 frames, pivot at the feet. Idle and the open hand sit here.
                var nx = 0.29f;
                var ny = 0.65f;
                if (actor.Pose == "Seal Bolt" && actor.Clip != null && actor.Clip.Length > 0 && actor.ClipT >= 0f)
                {
                    var u = actor.ClipDur > 0.01f ? actor.ClipT / actor.ClipDur : 1f;
                    var frame = Mathf.Clamp(Mathf.FloorToInt(u * actor.Clip.Length), 0, actor.Clip.Length - 1);
                    if (frame == 1)
                    {
                        nx = 0.27f;
                        ny = 0.68f;
                    }
                    else if (frame == 2)
                    {
                        nx = 0.30f;
                        ny = 0.73f;
                    }
                }
                return feet + new Vector2(face * w * nx, h * ny);
            }
            return feet + new Vector2(0f, 48f);
        }

        void PlaceSigilShard(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            // The sim bolt leaves the feet. The picture leaves the cyan crystal.
            var feet = OnSand(bolt.Src.X, bolt.Src.Y, width, height);
            var hand = Crystal(bolt.Src, feet);
            var tip = at + (hand - feet);
            var delta = tip - hand;
            var len = delta.magnitude;
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var mid = (hand + tip) * 0.5f;
            var cyan = new Color(0.35f, 0.86f, 1f, 1f);
            var hot = new Color(0.9f, 0.98f, 1f, 1f);
            var gold = new Color(1f, 0.86f, 0.35f, 1f);
            if (len > 12f)
            {
                Place(sigilBolt, mid, new Vector2(len, 22f), ang, cyan, 0.95f, false);
                Place(sigilLance, mid, new Vector2(len, 8f), ang, gold, 1f, false);
            }
            Place(sigilBolt, tip, new Vector2(48f, 18f), ang, hot, 1f, false);
            Place(sigilEmber, tip, new Vector2(22f, 22f), 0f, cyan, 1f, false);
            Place(sigilEmber, hand, new Vector2(36f, 36f), 0f, cyan, 1f, false);
            var flash = Mathf.Clamp01(1f - len / 180f);
            if (flash > 0.02f)
                Place(sigilBurst, hand, new Vector2(Mathf.Lerp(40f, 88f, flash), Mathf.Lerp(40f, 88f, flash)), 0f, cyan, 0.45f + 0.4f * flash, false);
        }

        Vector2 Crystal(ArenaBody body, Vector2 feet)
        {
            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Body != body || actor.Rect == null)
                    continue;
                var face = actor.Rect.localScale.x >= 0f ? 1f : -1f;
                var w = actor.Rect.sizeDelta.x;
                var h = actor.Rect.sizeDelta.y;
                // Measured on the 1329x1177 idle, pivot at the feet. The thrust frame sits further forward.
                var nx = 0.254f;
                var ny = 0.628f;
                if (actor.Pose == "Sigil Shard" && actor.Clip != null && actor.Clip.Length > 0 && actor.ClipT >= 0f)
                {
                    var u = actor.ClipDur > 0.01f ? actor.ClipT / actor.ClipDur : 1f;
                    var frame = Mathf.Clamp(Mathf.FloorToInt(u * actor.Clip.Length), 0, actor.Clip.Length - 1);
                    if (frame == 1)
                    {
                        nx = 0.317f;
                        ny = 0.681f;
                    }
                }
                return feet + new Vector2(face * w * nx, h * ny);
            }
            return feet + new Vector2(0f, 48f);
        }

        void PlacePathArrow(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            // The sim bolt leaves the feet. The picture leaves the bow.
            var feet = OnSand(bolt.Src.X, bolt.Src.Y, width, height);
            var hand = Bow(bolt.Src, feet);
            var tip = at + (hand - feet);
            var delta = tip - hand;
            var len = delta.magnitude;
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var mid = (hand + tip) * 0.5f;
            var green = new Color(0.29f, 0.87f, 0.50f, 1f);
            var hot = new Color(0.92f, 1f, 0.86f, 1f);
            if (len > 12f)
            {
                Place(sigilBolt, mid, new Vector2(len, 10f), ang, green, 1f, false);
                Place(sigilLance, mid, new Vector2(len, 4f), ang, hot, 1f, false);
            }
            Place(sigilBolt, tip, new Vector2(28f, 10f), ang, hot, 1f, false);
            Place(sigilEmber, hand, new Vector2(16f, 16f), 0f, green, 0.85f, false);
        }

        Vector2 Bow(ArenaBody body, Vector2 feet)
        {
            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Body != body || actor.Rect == null)
                    continue;
                var face = actor.Rect.localScale.x >= 0f ? 1f : -1f;
                var w = actor.Rect.sizeDelta.x;
                var h = actor.Rect.sizeDelta.y;
                // Measured on the 1091x969 canvas, pivot at the feet. Each aim uses that key's arrow tip.
                var nx = 0.462f;
                var ny = 0.589f;
                if (actor.Clip != null && actor.Clip.Length > 0 && actor.ClipT >= 0f)
                {
                    var u = actor.ClipDur > 0.01f ? actor.ClipT / actor.ClipDur : 1f;
                    var frame = Mathf.Clamp(Mathf.FloorToInt(u * actor.Clip.Length), 0, actor.Clip.Length - 1);
                    if (frame == 1 && actor.Pose == "Path Arrow")
                    {
                        nx = 0.379f;
                        ny = 0.682f;
                    }
                    else if (frame == 1 && actor.Pose == "Pinning Volley")
                    {
                        nx = 0.307f;
                        ny = 0.747f;
                    }
                    else if (frame == 1 && actor.Pose == "Needle Rain")
                    {
                        nx = 0.311f;
                        ny = 0.778f;
                    }
                }
                return feet + new Vector2(face * w * nx, h * ny);
            }
            return feet + new Vector2(0f, 48f);
        }

        void PlaceRing(Vector2 at, float diameter, Color color, float alpha, bool behind)
        {
            var wide = new Vector2(diameter, diameter * 0.42f);
            Place(sigilRingBack, at, wide, 0f, color, alpha * 0.85f, true);
            Place(sigilRingFront, at, wide, 0f, color, alpha, false);
            if (behind)
            {
                var back = _sigils[_sigilN - 2];
                back.Rect.SetSiblingIndex(0);
            }
        }

        void Place(Material mat, Vector2 at, Vector2 size, float angle, Color color, float alpha, bool behind)
        {
            if (mat == null || sigilGlow == null)
                return;
            var node = TakeSigil();
            node.Image.sprite = sigilGlow;
            node.Image.material = mat;
            node.Image.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            var rect = node.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = at;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
            if (behind)
                rect.SetSiblingIndex(0);
            else
                rect.SetAsLastSibling();
        }

        Sigil TakeSigil()
        {
            if (_sigilN >= _sigils.Count)
            {
                var go = new GameObject("Sigil", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(plate, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _sigils.Add(new Sigil { Rect = go.GetComponent<RectTransform>(), Image = image });
            }
            var node = _sigils[_sigilN];
            _sigilN++;
            node.Image.enabled = true;
            return node;
        }

        float WorldSpan(double world, float plateW)
        {
            return (float)(world / ArenaMatch.WorldW) * (SandRight - SandLeft) * plateW;
        }

        Color KitColor(ArenaBody body)
        {
            if (body.Token || (body.Id != null && body.Id.StartsWith("ward_")))
                return new Color(0.55f, 0.86f, 1f, 1f);
            var kit = LaneRoster.ById(body.Id);
            return string.IsNullOrEmpty(kit.Color) ? Gold : Hex(kit.Color);
        }

        static ArenaBody NearestFoe(ArenaBody body)
        {
            ArenaBody best = null;
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
                    best = other;
                }
            }
            return best;
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
                return bruteRest != null ? bruteRest : bruteStill;
            if (id == "lightfather")
                return lightfatherStill;
            if (id == "kael")
                return kaelStill;
            if (id == "orin")
                return orinStill;
            if (id == "nia")
                return niaStill;
            return null;
        }

        void EnsureActors()
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                var found = false;
                for (var a = 0; a < _actors.Count; a++)
                {
                    if (_actors[a].Body == body)
                        found = true;
                }
                if (!found)
                {
                    AddActor(body);
                    if (ArenaMatch.Time > 0.4)
                        Push(3, body.X, body.Y, body.X, body.Y, 0.45f, 90f, KitColor(body));
                }
            }
        }

        void AddActor(ArenaBody body)
        {
            var actor = new Actor { Body = body };
            var node = new GameObject(body.Id, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(plate, false);
            actor.Rect = node.GetComponent<RectTransform>();
            actor.Image = node.GetComponent<Image>();
            actor.Image.raycastTarget = false;
            actor.Image.preserveAspect = true;
            actor.SeenHp = body.Hp;
            actor.SeenAlive = body.Alive;
            var painted = Still(body.Id);
            if (painted != null)
            {
                actor.Image.sprite = painted;
                actor.Image.color = Color.white;
            }
            else
            {
                actor.Image.sprite = mark;
                actor.Image.color = body.Token ? new Color(0.45f, 0.75f, 0.95f, 1f) : Hex(LaneRoster.ById(body.Id).Color);
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
            if (!body.Token)
                actor.Hud = Column(TeamIndex(body), body.Team == 0);
            _actors.Add(actor);
        }

        void AdvanceClip(Actor actor, float dt)
        {
            if (actor.ClipT < 0f)
                return;
            actor.ClipT += dt;
            if (actor.ClipT < actor.ClipDur)
                return;
            if (actor.HoldClip)
                actor.ClipT = actor.ClipDur - 0.0001f;
            else
            {
                actor.ClipT = -1f;
                actor.Pose = "";
            }
        }

        void Arm(Actor actor, ArenaBody body)
        {
            var call = body.Callout ?? "";
            if (body.Id == "vale")
            {
                if (call == "Shield Bash" || call == "Shield Hook")
                    ArmClip(actor, valeBash, 0.4f, 0.39f, call);
                else if (call == "Cleave" || call == "Punch")
                    ArmClip(actor, valePunch, 0.34f, 0.37f, call);
                else if (call == "Cover")
                    ArmClip(actor, valeToss, 0.5f, 0.3f, call);
                else if (call == "Bulwark")
                    ArmClip(actor, valeLine, 0.5f, 0.54f, call);
                else if (call == "Shield Wall")
                    ArmClip(actor, valePlant, 0.4f, 0.56f, call);
            }
            else if (body.Id == "brute")
            {
                if (call == "Heavy Sweep")
                    ArmClip(actor, bruteSweep, 1.42f, 0f, call);
                else if (call == "Ground Pound")
                    ArmClip(actor, brutePound, 0.7f, 0f, call);
                else if (call == "Ward Field")
                    ArmClip(actor, bruteWard, 0.55f, 0f, call);
                else if (call == "Close" || call == "Slip")
                    ArmClip(actor, bruteClose, 0.35f, 0f, call);
                else if (call == "Iron Grip")
                    ArmClip(actor, bruteGrip == null ? null : new[] { bruteGrip }, 0.4f, 0f, call);
            }
            else if (body.Id == "lightfather")
            {
                if (call == "Seal Bolt")
                    ArmClip(actor, lfBolt, 0.42f, 0.5f, call);
                else if (call == "Δ9 Seal")
                    ArmClip(actor, lfSeal, 0.70f, 0.5f, call);
                else if (call == "Provenance")
                    ArmClip(actor, lfProvenance, 0.80f, 0.5f, call);
                else if (call == "Cover")
                    ArmClip(actor, lfCover, 0.50f, 0.5f, call);
                else if (call == "Standard Raised")
                    ArmClip(actor, lfStandard, 0.55f, 0.5f, call);
                else if (call == "Slip" || call == "Close")
                    ArmClip(actor, lfSlip, 0.35f, 0.5f, call);
            }
            else if (body.Id == "kael")
            {
                if (call == "Shard Volley")
                    ArmClip(actor, kaelShard, 0.72f, 0.5f, call);
                else if (call == "Rend")
                    ArmClip(actor, kaelRend, 0.42f, 0.5f, call);
                else if (call == "Shield Wall")
                    ArmClip(actor, kaelWall, 0.50f, 0.5f, call);
                else if (call == "Squire's Charge")
                    ArmClip(actor, kaelCharge, 0.40f, 0.5f, call);
                else if (call == "Close" || call == "Slip")
                    ArmClip(actor, kaelClose, 0.35f, 0.5f, call);
            }
            else if (body.Id == "orin")
            {
                if (call == "Sigil Shard")
                    ArmClip(actor, orinShard, 0.36f, 0.5f, call);
                else if (call == "Unwrite")
                    ArmClip(actor, orinUnwrite, 0.45f, 0.5f, call);
                else if (call == "Resonance Flask")
                    ArmClip(actor, orinFlask, 0.45f, 0.5f, call);
                else if (call == "Chorus Flask")
                    ArmClip(actor, orinDrink, 0.50f, 0.5f, call);
                else if (call == "Slip" || call == "Close")
                    ArmClip(actor, orinSlip, 0.35f, 0.5f, call);
            }
            else if (body.Id == "nia")
            {
                if (call == "Path Arrow")
                    ArmClip(actor, niaArrow, 0.50f, 0.5f, call);
                else if (call == "Pinning Volley")
                    ArmClip(actor, niaPin, 0.52f, 0.5f, call);
                else if (call == "Dash")
                    ArmClip(actor, niaDash, 0.42f, 0.5f, call);
                else if (call == "Needle Rain")
                    ArmClip(actor, niaRain, 0.52f, 0.5f, call);
                else if (call == "Slip" || call == "Close")
                    ArmClip(actor, niaSlip, 0.35f, 0.5f, call);
            }
        }

        void ArmEnd(Actor actor, ArenaBody body, bool win)
        {
            if (body.Id == "vale")
                ArmClip(actor, win ? valeWin : valeDeath, win ? 1.15f : 1.6f, win ? 0.40f : 0.42f, win ? "win" : "death");
            else if (body.Id == "brute")
                ArmClip(actor, win ? bruteWin : bruteDeath, win ? 1.15f : 1.55f, 0f, win ? "win" : "death");
            else if (body.Id == "lightfather")
                ArmClip(actor, win ? lfWin : lfDeath, win ? 1.15f : 1.55f, 0.5f, win ? "win" : "death");
            else if (body.Id == "kael")
                ArmClip(actor, win ? kaelWin : kaelDeath, win ? 1.15f : 1.55f, 0.5f, win ? "win" : "death");
            else if (body.Id == "orin")
                ArmClip(actor, win ? orinWin : orinDeath, win ? 1.15f : 1.55f, 0.5f, win ? "win" : "death");
            else if (body.Id == "nia")
                ArmClip(actor, win ? niaWin : niaDeath, win ? 1.15f : 1.55f, 0.5f, win ? "win" : "death");
            else
                actor.Pose = win ? "win" : "death";
            actor.HoldClip = true;
        }

        static void ArmClip(Actor actor, Sprite[] frames, float duration, float pivot, string pose)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
                return;
            actor.Clip = frames;
            actor.ClipDur = duration;
            actor.ClipT = 0f;
            actor.Pivot = pivot;
            actor.Pose = pose;
        }

        void ShowFrame(Actor actor, ArenaBody body)
        {
            if (actor.Image == null)
                return;
            if (actor.ClipT >= 0f && actor.Clip != null && actor.Clip.Length > 0)
            {
                var u = actor.ClipDur > 0.01f ? actor.ClipT / actor.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * actor.Clip.Length), 0, actor.Clip.Length - 1);
                if (body.Id == "brute" && body.Callout == "Heavy Sweep" && !actor.HoldClip)
                {
                    var last = actor.Clip.Length - 1;
                    frame = u >= 0.72f ? last : Mathf.Clamp(Mathf.FloorToInt((u / 0.72f) * last), 0, last);
                }
                if (actor.Clip[frame] != null)
                    actor.Image.sprite = actor.Clip[frame];
                return;
            }
            if (actor.WalkT >= 0f)
            {
                var frames = WalkOf(body.Id);
                if (frames != null && frames.Length > 0)
                {
                    var dur = WalkDur(body.Id);
                    var u = dur > 0.01f ? (actor.WalkT % dur) / dur : 0f;
                    var frame = Mathf.Clamp(Mathf.FloorToInt(u * frames.Length), 0, frames.Length - 1);
                    if (frames[frame] != null)
                    {
                        actor.Image.sprite = frames[frame];
                        return;
                    }
                }
            }
            var still = Still(body.Id);
            if (still != null)
                actor.Image.sprite = still;
        }

        void AdvanceWalk(Actor actor, float dt)
        {
            var body = actor.Body;
            if (body == null)
                return;
            var moved = false;
            if (actor.HasFoot && body.Alive)
            {
                var dx = body.X - actor.FootX;
                var dy = body.Y - actor.FootY;
                moved = dx * dx + dy * dy > 2.25;
                var named = body.Callout ?? "";
                if ((named == "Close" || named == "Slip") && dx * dx + dy * dy > 64 * 64)
                {
                    Push(4, actor.FootX, actor.FootY, body.X, body.Y, 0.32f, 26f, KitColor(body));
                    Push(3, actor.FootX, actor.FootY, actor.FootX, actor.FootY, 0.28f, 72f, KitColor(body));
                }
            }
            actor.HasFoot = true;
            actor.FootX = body.X;
            actor.FootY = body.Y;
            var frames = WalkOf(body.Id);
            if (actor.ClipT >= 0f || !moved || frames == null || frames.Length == 0)
            {
                actor.WalkT = -1f;
                return;
            }
            if (actor.WalkT < 0f)
                actor.WalkT = 0f;
            actor.WalkT += dt;
        }

        Sprite[] WalkOf(string id)
        {
            if (id == "vale")
                return valeWalk;
            if (id == "brute")
                return bruteWalk;
            if (id == "lightfather")
                return lfWalk;
            if (id == "kael")
                return kaelWalk;
            if (id == "orin")
                return orinWalk;
            if (id == "nia")
                return niaWalk;
            return null;
        }

        static float WalkDur(string id)
        {
            if (id == "brute")
                return 0.84f;
            if (id == "lightfather")
                return 0.72f;
            if (id == "kael")
                return 0.76f;
            if (id == "orin")
                return 0.80f;
            if (id == "nia")
                return 0.58f;
            return 0.62f;
        }

        void BuildChrome()
        {
            var clockPlate = new GameObject("ClockPlate", typeof(RectTransform), typeof(Image));
            clockPlate.transform.SetParent(transform, false);
            var clockBox = clockPlate.GetComponent<RectTransform>();
            clockBox.anchorMin = new Vector2(0.445f, 0.915f);
            clockBox.anchorMax = new Vector2(0.555f, 0.965f);
            clockBox.offsetMin = clockBox.offsetMax = Vector2.zero;
            var clockFace = clockPlate.GetComponent<Image>();
            clockFace.raycastTarget = false;
            if (barFrame != null)
            {
                clockFace.sprite = barFrame;
                clockFace.type = Image.Type.Sliced;
                clockFace.color = Color.white;
            }
            else
                clockFace.color = new Color(0.08f, 0.06f, 0.04f, 0.8f);
            _clock = Words(clockPlate.transform, "0:00");
            Stretch(_clock.rectTransform);
            _clock.alignment = TextAnchor.MiddleCenter;
            _clock.fontSize = 18;
            _callout = Words(transform, "Colosseum  ·  swept sand");
            var call = _callout.rectTransform;
            call.anchorMin = new Vector2(0.30f, 0.868f);
            call.anchorMax = new Vector2(0.70f, 0.908f);
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
