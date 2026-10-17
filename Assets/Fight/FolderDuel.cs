using System.Collections;
using System.Collections.Generic;
using BattleTv.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace BattleTv.Fight
{
    /// <summary>
    /// A painted 1v1 that is not the baked Vale versus Brute card.
    /// The book is ArenaLogic. The pictures come from each fighter folder.
    /// </summary>
    public sealed class FolderDuel : MonoBehaviour
    {
        const float Band = 720f / 1152f;
        const float Feet = (216f + 0.2f * 720f - 36f) / 1152f;

        DuelView _view;
        bool _ready;
        bool _board;
        int _sigilN;
        AudioSource _voice;
        readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>(8);
        readonly List<Slot> _slots = new List<Slot>(6);
        readonly List<Sigil> _sigils = new List<Sigil>(48);
        readonly List<Flash> _flashes = new List<Flash>(24);
        Material _ringBack;
        Material _ringFront;
        Material _bolt;
        Material _burst;
        Material _mend;
        Material _ember;
        Material _lance;

        sealed class Slot
        {
            public ArenaBody Body;
            public RectTransform Rect;
            public Image Image;
            public Sprite[] Clip;
            public float ClipT = -1f;
            public float ClipDur = 0.4f;
            public float Pivot = 0.5f;
            public float Pace;
            public bool HoldClip;
            public string Pose = "";
            public double SeenHp = -1;
            public bool SeenAlive = true;
            public float WalkT = -1f;
            public bool HasFoot;
            public double FootX;
            public double FootY;
            public double Heard;
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

        IEnumerator Start()
        {
            _view = GetComponent<DuelView>();
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            var shader = Shader.Find("BattleTv/ArenaSigil");
            if (shader != null)
            {
                _ringBack = Make(shader, 1f, 1f);
                _ringFront = Make(shader, 1f, -1f);
                _bolt = Make(shader, 0f, 0f);
                _burst = Make(shader, 3f, 0f);
                _mend = Make(shader, 2f, 0f);
                _ember = Make(shader, 5f, 0f);
                _lance = Make(shader, 4f, 0f);
            }
            var yours = ShowPick.Yours.ToArray();
            var foes = ShowPick.Foes.ToArray();
            var ids = new List<string>(yours.Length + foes.Length);
            ids.AddRange(yours);
            ids.AddRange(foes);
            yield return FighterBodies.Load(ids);
            ArenaMatch.Begin(yours, foes, UnityEngine.Random.Range(1, 9999), true);
            Spawn();
            Play("fight", 0.5f);
            _ready = true;
        }

        void OnDestroy()
        {
            DestroyMat(_ringBack);
            DestroyMat(_ringFront);
            DestroyMat(_bolt);
            DestroyMat(_burst);
            DestroyMat(_mend);
            DestroyMat(_ember);
            DestroyMat(_lance);
        }

        static Material Make(Shader shader, float kind, float half)
        {
            var mat = new Material(shader);
            mat.SetFloat("_Kind", kind);
            mat.SetFloat("_Half", half);
            return mat;
        }

        static void DestroyMat(Material mat)
        {
            if (mat != null)
                Destroy(mat);
        }

        void Update()
        {
            if (_view != null)
                _view.Fit();
            if (!_ready || _view == null || _view.StagePlate == null)
                return;
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (!_board)
            {
                var before = Alive();
                ArenaMatch.Step(dt);
                if (Alive() < before)
                    Play("kill", 0.5f);
            }
            for (var i = 0; i < _slots.Count; i++)
            {
                AdvanceClip(_slots[i], dt);
                AdvanceWalk(_slots[i], dt);
            }
            EnsureSlots();
            Paint();
            AgeFlashes(dt);
            if (ArenaMatch.Over && !_board)
                OpenBoard();
        }

        void Spawn()
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
                AddSlot(ArenaMatch.Bodies[i]);
        }

        void EnsureSlots()
        {
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                var found = false;
                for (var s = 0; s < _slots.Count; s++)
                {
                    if (_slots[s].Body == body)
                        found = true;
                }
                if (!found)
                    AddSlot(body);
            }
        }

        void AddSlot(ArenaBody body)
        {
            var slot = new Slot { Body = body, SeenHp = body.Hp, SeenAlive = body.Alive };
            var node = new GameObject(body.Id, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(_view.StagePlate, false);
            slot.Rect = node.GetComponent<RectTransform>();
            slot.Image = node.GetComponent<Image>();
            slot.Image.raycastTarget = false;
            slot.Image.preserveAspect = true;
            var still = Still(body.Id);
            slot.Image.sprite = still;
            slot.Image.color = still != null ? Color.white : Hex(LaneRoster.ById(body.Id).Color);
            if (still == null && _view.Glow != null)
                slot.Image.sprite = _view.Glow;
            _slots.Add(slot);
        }

        void Paint()
        {
            var plate = _view.StagePlate;
            var width = plate.rect.width;
            var height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            for (var i = 0; i < _slots.Count; i++)
                PlaceBody(_slots[i], width, height);
            Hud();
            Clock();
            Callout();
            PaintSigils(width, height);
        }

        void PlaceBody(Slot slot, float width, float height)
        {
            var body = slot.Body;
            var point = OnLane(body.X, width, height);
            var spriteH = height * Band * (body.Id == "brute" ? 0.56f : 0.5f);
            var aspect = 0.72f;
            if (slot.Image.sprite != null && slot.Image.sprite.rect.height > 1f)
                aspect = slot.Image.sprite.rect.width / slot.Image.sprite.rect.height;
            slot.Rect.anchorMin = slot.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            slot.Rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            slot.Rect.anchoredPosition = point;
            var foe = NearestX(body);
            var painted = FighterBodies.Get(body.Id);
            var face = 1f;
            if (painted != null && painted.FlipWithFoe)
                face = foe >= body.X ? -1f : 1f;
            else
                face = foe >= body.X ? 1f : -1f;
            if (body.Alive)
                slot.Rect.localScale = new Vector3(face, 1f, 1f);
            if (body.Swing > 0.22 && slot.Heard < 0.05)
            {
                var named = body.Callout ?? "";
                Play(named == "Close" || named == "Slip" ? "cast" : "hit", 0.22f);
            }
            if (body.Alive && body.Swing > slot.Heard + 0.02 && body.Swing > 0.18)
            {
                Arm(slot, body);
                NoteCast(body);
            }
            if (!body.Alive && slot.Pose != "death")
                ArmEnd(slot, body, false);
            if (ArenaMatch.Over && body.Alive && slot.Pose != "win" && slot.Pose != "death")
                ArmEnd(slot, body, true);
            slot.Heard = body.Swing;
            ShowFrame(slot, body);
            var pivot = painted != null && painted.ClipPivot && slot.ClipT >= 0f
                ? slot.Pivot
                : PivotX(body.Id, slot.Image);
            slot.Rect.pivot = new Vector2(pivot, 0f);
            var has = Still(body.Id) != null;
            slot.Image.color = body.Alive
                ? (has ? Color.white : Hex(LaneRoster.ById(body.Id).Color))
                : (has ? new Color(0.78f, 0.7f, 0.62f, 1f) : new Color(0.35f, 0.28f, 0.22f, 0.8f));
            slot.Rect.SetAsLastSibling();
        }

        void Hud()
        {
            ArenaBody left = null;
            ArenaBody right = null;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                if (body.Token)
                    continue;
                if (body.Team == 0 && left == null)
                    left = body;
                if (body.Team == 1 && right == null)
                    right = body;
            }
            var fists = (left == null || !left.Gun) && (right == null || !right.Gun);
            if (_view.LeftHud != null && left != null)
                _view.LeftHud.ShowPools(left.Name, left.Cards, left.Hp, left.Max, left.Mana, left.ManaMax, left.AtkCd, left.Rate, left.Shield, left.StrideCd, fists);
            if (_view.RightHud != null && right != null)
                _view.RightHud.ShowPools(right.Name, right.Cards, right.Hp, right.Max, right.Mana, right.ManaMax, right.AtkCd, right.Rate, right.Shield, right.StrideCd, fists);
        }

        void Clock()
        {
            var label = _view.ClockLabel;
            if (label == null)
                return;
            var secs = Mathf.FloorToInt((float)ArenaMatch.Time);
            var face = (secs / 60) + ":" + (secs % 60).ToString("00");
            if (ArenaMatch.Time >= SideDuel.SuddenAt)
            {
                var pct = (int)System.Math.Round((SideDuel.SuddenScale(ArenaMatch.Time) - 1) * 100);
                face += "  +" + pct;
            }
            label.text = face;
            label.color = ArenaMatch.Time >= SideDuel.SuddenAt
                ? new Color(0.98f, 0.75f, 0.14f, 1f)
                : new Color(0.965f, 0.906f, 0.757f, 1f);
        }

        void Callout()
        {
            var label = _view.CalloutLabel;
            if (label == null || _board)
                return;
            ArenaBody loud = null;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                if (body.Token || string.IsNullOrEmpty(body.Callout) || body.Swing <= 0.02)
                    continue;
                if (loud == null || body.Swing > loud.Swing)
                    loud = body;
            }
            if (loud != null)
                label.text = loud.Name + "  " + loud.Callout;
        }

        void OpenBoard()
        {
            _board = true;
            var title = (ArenaMatch.Title ?? "").Replace("sand", "lane");
            var detail = (ArenaMatch.Detail ?? "").Replace("sand", "lane");
            ShowPick.RememberResult(WinnerId());
            Play("win", 0.55f);
            _view.ShowFolderBoard(title, detail);
        }

        static string WinnerId()
        {
            var leftUp = false;
            var rightUp = false;
            string left = null;
            string right = null;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var body = ArenaMatch.Bodies[i];
                if (body.Team == 0)
                {
                    if (!body.Token && left == null)
                        left = body.Id;
                    if (body.Alive)
                        leftUp = true;
                }
                else if (body.Team == 1)
                {
                    if (!body.Token && right == null)
                        right = body.Id;
                    if (body.Alive)
                        rightUp = true;
                }
            }
            if (leftUp && !rightUp)
                return left;
            if (rightUp && !leftUp)
                return right;
            return null;
        }

        static Vector2 OnLane(double x, float width, float height)
        {
            var px = ((float)(x / ArenaMatch.WorldW) - 0.5f) * width;
            var feet = -height * 0.5f + height * Feet;
            return new Vector2(px, feet);
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
                var d = (other.X - body.X) * (other.X - body.X);
                if (d < bestD)
                {
                    bestD = d;
                    best = other.X;
                }
            }
            return best;
        }

        static Sprite Still(string id)
        {
            var body = FighterBodies.Get(id);
            return body != null ? body.Idle : null;
        }

        static float WalkDur(string id)
        {
            var body = FighterBodies.Get(id);
            return body != null ? body.WalkSeconds : 0.62f;
        }

        static Sprite[] WalkOf(string id)
        {
            var body = FighterBodies.Get(id);
            return body != null && body.Walk != null ? body.Walk.Sprites : null;
        }

        static float PivotX(string id, Image image)
        {
            if (id == "brute" && image != null && image.sprite != null && image.sprite.rect.width > 1f)
                return image.sprite.pivot.x / image.sprite.rect.width;
            return 0.5f;
        }

        static void AdvanceClip(Slot slot, float dt)
        {
            if (slot.ClipT < 0f)
                return;
            slot.ClipT += dt;
            if (slot.ClipT < slot.ClipDur)
                return;
            if (slot.HoldClip)
                slot.ClipT = slot.ClipDur - 0.0001f;
            else
            {
                slot.ClipT = -1f;
                slot.Pose = "";
            }
        }

        static void Arm(Slot slot, ArenaBody body)
        {
            var painted = FighterBodies.Get(body.Id);
            var clip = painted != null ? painted.Find(body.Callout) : null;
            if (clip == null)
                return;
            ArmClip(slot, clip.Sprites, clip.Seconds, clip.Pivot, clip.Pace, body.Callout);
        }

        static void ArmEnd(Slot slot, ArenaBody body, bool win)
        {
            var painted = FighterBodies.Get(body.Id);
            var clip = painted != null ? painted.End(win) : null;
            if (clip != null)
                ArmClip(slot, clip.Sprites, clip.Seconds, clip.Pivot, clip.Pace, win ? "win" : "death");
            else
                slot.Pose = win ? "win" : "death";
            slot.HoldClip = true;
        }

        static void ArmClip(Slot slot, Sprite[] frames, float duration, float pivot, float pace, string pose)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
                return;
            slot.Clip = frames;
            slot.ClipDur = duration;
            slot.ClipT = 0f;
            slot.Pivot = pivot;
            slot.Pace = pace;
            slot.Pose = pose;
        }

        static void ShowFrame(Slot slot, ArenaBody body)
        {
            if (slot.Image == null)
                return;
            if (slot.ClipT >= 0f && slot.Clip != null && slot.Clip.Length > 0)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (slot.Pace > 0.01f && !slot.HoldClip)
                {
                    var last = slot.Clip.Length - 1;
                    frame = u >= slot.Pace ? last : Mathf.Clamp(Mathf.FloorToInt((u / slot.Pace) * last), 0, last);
                }
                if (slot.Clip[frame] != null)
                    slot.Image.sprite = slot.Clip[frame];
                return;
            }
            if (slot.WalkT >= 0f)
            {
                var frames = WalkOf(body.Id);
                if (frames != null && frames.Length > 0)
                {
                    var dur = WalkDur(body.Id);
                    var u = dur > 0.01f ? (slot.WalkT % dur) / dur : 0f;
                    var frame = Mathf.Clamp(Mathf.FloorToInt(u * frames.Length), 0, frames.Length - 1);
                    if (frames[frame] != null)
                    {
                        slot.Image.sprite = frames[frame];
                        return;
                    }
                }
            }
            var still = Still(body.Id);
            if (still != null)
                slot.Image.sprite = still;
        }

        void AdvanceWalk(Slot slot, float dt)
        {
            var body = slot.Body;
            if (body == null)
                return;
            var moved = false;
            if (slot.HasFoot && body.Alive)
            {
                var dx = body.X - slot.FootX;
                moved = dx * dx > 2.25;
                var named = body.Callout ?? "";
                if ((named == "Close" || named == "Slip") && dx * dx > 64 * 64)
                {
                    Push(4, slot.FootX, slot.FootY, body.X, body.Y, 0.32f, 26f, KitColor(body));
                    Push(3, slot.FootX, slot.FootY, slot.FootX, slot.FootY, 0.28f, 72f, KitColor(body));
                }
            }
            slot.HasFoot = true;
            slot.FootX = body.X;
            slot.FootY = body.Y;
            var frames = WalkOf(body.Id);
            if (slot.ClipT >= 0f || !moved || frames == null || frames.Length == 0)
            {
                slot.WalkT = -1f;
                return;
            }
            if (slot.WalkT < 0f)
                slot.WalkT = 0f;
            slot.WalkT += dt;
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
                if (body.Id == "d9ra" && shape == "aura")
                    Push(1, body.X, body.Y, body.X, body.Y, 0.42f, radius, color);
                if (body.Id == "kairos" && mv != null && (mv.Id == "righttime" || mv.Id == "seal9"))
                    Push(1, at.X, at.Y, at.X, at.Y, 0.55f, radius, color);
                if (body.Id == "seidon" && mv != null && mv.Id == "undertow")
                    Push(1, at.X, at.Y, at.X, at.Y, 0.55f, radius, color);
            }
            else if (shape == "beam" && foe != null)
                Push(4, body.X, body.Y, foe.X, foe.Y, 0.32f, 18f, color);
            else if (shape == "melee" || shape == "arc")
            {
                if (foe != null)
                {
                    Push(5, foe.X, foe.Y, foe.X, foe.Y, 0.22f, 36f, color);
                    if (body.Id == "justicae" && mv != null && mv.Id == "verdict")
                        Push(1, foe.X, foe.Y, foe.X, foe.Y, 0.28f, 70f, color);
                }
            }
            else if (shape == "bolt" || shape == "multi" || shape == "lob")
                Push(5, body.X, body.Y, body.X, body.Y, 0.14f, 28f, color);
            else if (shape == "revive")
                Push(2, body.X, body.Y, body.X, body.Y, 0.7f, 120f, color);
            else if (shape == "blink" || shape == "stride")
            {
                Push(3, body.X, body.Y, body.X, body.Y, 0.35f, 80f, color);
                if (body.Id == "justicae" && mv != null && mv.Id == "writpursuit")
                    Push(1, body.X, body.Y, body.X, body.Y, 0.35f, 90f, color);
            }
            else if (body.Id == "d9ra" && mv != null && mv.Id == "wolffury")
                Push(1, body.X, body.Y, body.X, body.Y, 0.5f, 110f, color);
            else
            {
                var ward = Has(st, self, true);
                Push(ward && !Has(st, self, false) ? 3 : 2, body.X, body.Y, body.X, body.Y, 0.45f, ward ? 110f : 80f, color);
                if (body.Id == "kairos" && mv != null && mv.Id == "borrow")
                    Push(1, body.X, body.Y, body.X, body.Y, 0.45f, 90f, color);
                if (body.Id == "justicae" && mv != null && (mv.Id == "fairplate" || mv.Id == "bulwark"))
                    Push(1, body.X, body.Y, body.X, body.Y, 0.50f, 100f, color);
                if (body.Id == "seidon" && mv != null && mv.Id == "current")
                    Push(1, body.X, body.Y, body.X, body.Y, 0.50f, 110f, color);
                if (body.Id == "sancora" && mv != null && (mv.Id == "chorusflask" || mv.Id == "weavebloom" || mv.Id == "cover"))
                    Push(1, body.X, body.Y, body.X, body.Y, 0.50f, 100f, color);
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

        static ArenaBody NearestFoe(ArenaBody body)
        {
            ArenaBody foe = null;
            var best = double.MaxValue;
            for (var i = 0; i < ArenaMatch.Bodies.Count; i++)
            {
                var other = ArenaMatch.Bodies[i];
                if (!other.Alive || other.Team == body.Team)
                    continue;
                var d = (other.X - body.X) * (other.X - body.X);
                if (d < best)
                {
                    best = d;
                    foe = other;
                }
            }
            return foe;
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
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var body = slot.Body;
                if (body == null)
                    continue;
                var color = KitColor(body);
                var feet = OnLane(body.X, width, height);
                var rise = slot.Rect != null ? slot.Rect.sizeDelta.y * 0.42f : 40f;
                if (!body.Alive && slot.SeenAlive)
                    Push(5, body.X, body.Y, body.X, body.Y, 0.28f, 48f, color);
                if (body.Alive && !slot.SeenAlive)
                    Push(2, body.X, body.Y, body.X, body.Y, 0.65f, 130f, color);
                if (body.Alive && slot.SeenHp >= 0 && body.Hp > slot.SeenHp + 0.75)
                    Push(2, body.X, body.Y, body.X, body.Y, 0.4f, 90f, color);
                if (body.Alive && slot.SeenHp >= 0 && body.Hp < slot.SeenHp - 0.75)
                    Push(5, body.X, body.Y, body.X, body.Y, 0.18f, 34f, color);
                slot.SeenHp = body.Hp;
                slot.SeenAlive = body.Alive;
                if (!body.Alive)
                    continue;
                if (body.Shield > 0.5 || body.ThornsT > 0.05)
                    PlaceRing(feet, body.ThornsT > 0.05 ? 150f : 124f, color, 0.9f, true);
                if (body.Stun > 0.05 || body.Root > 0.05)
                    PlaceRing(feet + new Vector2(0f, rise), 70f, color, 0.75f, false);
                if (body.Id == "d9ra" && body.RageT > 0.05)
                    PlaceRing(feet, 110f, color, 0.7f, true);
                if (body.Id == "srath" && body.VeilT > 0.05)
                    PlaceRing(feet, 96f, color, 0.65f, true);
                if (body.RegenT > 0.05 || body.HasteT > 0.05 || body.SwiftT > 0.05 || body.VeilT > 0.05)
                    Place(_mend, feet + new Vector2(0f, rise * 0.45f), new Vector2(78f, 36f), 0f, color, 0.55f, false);
                if (body.BurnT > 0.05 || body.BleedT > 0.05)
                    Place(_ember, feet + new Vector2(0f, rise), new Vector2(30f, 30f), 0f, color, 0.8f, false);
            }
            for (var i = 0; i < ArenaMatch.Bolts.Count; i++)
            {
                var bolt = ArenaMatch.Bolts[i];
                var color = bolt.Src != null ? KitColor(bolt.Src) : new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
                var at = OnLane(bolt.X, width, height);
                if (bolt.Lob && bolt.Src != null && bolt.Src.Id == "arkos")
                    PlaceRing(at, 48f, color, 1f, false);
                else if (bolt.Lob && bolt.Src != null && bolt.Src.Id == "seidon")
                    PlaceRing(at, 64f, new Color(0.13f, 0.83f, 0.93f, 1f), 1f, false);
                else if (bolt.Lob)
                    Place(_ember, at, new Vector2(26f, 26f), 0f, color, 1f, false);
                else if (bolt.Src != null && bolt.Src.Id == "lightfather")
                    PlaceSealBolt(bolt, at, width, height, color);
                else if (bolt.Src != null && bolt.Src.Id == "orin")
                    PlaceSigilShard(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "nia")
                    PlacePathArrow(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "lyra")
                    PlaceSpiralNote(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "srath")
                    PlaceNeedle(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "kairos")
                    PlaceHourBolt(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "seidon")
                    PlaceTideBolt(bolt, at, width, height);
                else if (bolt.Src != null && bolt.Src.Id == "sancora")
                    PlaceFlask(bolt, at, width, height);
                else
                    Place(_bolt, at, new Vector2(46f, 14f), -(float)bolt.Angle * Mathf.Rad2Deg, color, 1f, false);
            }
            for (var i = 0; i < _flashes.Count; i++)
            {
                var flash = _flashes[i];
                var k = flash.Life / Mathf.Max(0.05f, flash.Max);
                var grow = flash.Kind == 3 ? Mathf.Lerp(0.45f, 1f, 1f - k) : 1f;
                var at = OnLane(flash.X, width, height);
                var size = (float)(flash.Radius / ArenaMatch.WorldW) * width * (flash.Kind == 3 ? 2f : 1f) * grow;
                size = Mathf.Clamp(size, 28f, 520f);
                if (flash.Kind == 4)
                {
                    var to = OnLane(flash.X2, width, height);
                    var delta = to - at;
                    var mid = (at + to) * 0.5f;
                    var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                    Place(_lance, mid, new Vector2(Mathf.Max(8f, delta.magnitude), 16f), ang, flash.Color, k, false);
                }
                else if (flash.Kind == 2)
                    Place(_mend, at + new Vector2(0f, size * 0.15f), new Vector2(size, size * 0.42f), 0f, flash.Color, k, false);
                else if (flash.Kind == 5)
                    Place(_ember, at + new Vector2(0f, size * 0.35f), new Vector2(size, size), 0f, flash.Color, k, false);
                else
                    PlaceRing(at, Mathf.Max(64f, size), flash.Color, k, false);
            }
            for (var i = _sigilN; i < _sigils.Count; i++)
                _sigils[i].Image.enabled = false;
        }

        void PlaceSealBolt(ArenaBolt bolt, Vector2 at, float width, float height, Color color)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var hand = Hand(bolt.Src, feet, "Seal Bolt", 0.29f, 0.65f, 0.27f, 0.68f, 0.30f, 0.73f);
            Shot(hand, at + (hand - feet), color, new Color(1f, 0.97f, 0.78f, 1f), 120f, 36f, 84f, 52f, 64f, 280f);
        }

        void PlaceSigilShard(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var hand = Hand(bolt.Src, feet, "Sigil Shard", 0.254f, 0.628f, 0.317f, 0.681f, 0.317f, 0.681f);
            var cyan = new Color(0.35f, 0.86f, 1f, 1f);
            Shot(hand, at + (hand - feet), cyan, new Color(0.9f, 0.98f, 1f, 1f), 22f, 8f, 48f, 18f, 36f, 180f);
        }

        void PlacePathArrow(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var nx = 0.462f;
            var ny = 0.589f;
            var slot = FindSlot(bolt.Src);
            if (slot != null && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1 && slot.Pose == "Path Arrow")
                {
                    nx = 0.379f;
                    ny = 0.682f;
                }
                else if (frame == 1 && slot.Pose == "Pinning Volley")
                {
                    nx = 0.307f;
                    ny = 0.747f;
                }
                else if (frame == 1 && slot.Pose == "Needle Rain")
                {
                    nx = 0.311f;
                    ny = 0.778f;
                }
            }
            var hand = Anchor(bolt.Src, feet, nx, ny);
            var green = new Color(0.29f, 0.87f, 0.50f, 1f);
            Shot(hand, at + (hand - feet), green, new Color(0.92f, 1f, 0.86f, 1f), 10f, 4f, 28f, 10f, 16f, 180f);
        }

        void PlaceSpiralNote(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var hand = Anchor(bolt.Src, feet, 0.320f, 0.679f);
            var cyan = new Color(0.40f, 0.91f, 0.98f, 1f);
            Shot(hand, at + (hand - feet), cyan, new Color(0.90f, 1f, 1f, 1f), 8f, 3f, 22f, 22f, 18f, 180f);
        }

        void PlaceNeedle(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var nx = 0.12f;
            var ny = 0.42f;
            var slot = FindSlot(bolt.Src);
            if (slot != null && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1 && slot.Pose == "Needle")
                {
                    nx = 0.412f;
                    ny = 0.754f;
                }
                else if (frame == 1 && slot.Pose == "Needle Rain")
                {
                    nx = -0.109f;
                    ny = 0.913f;
                }
            }
            var hand = Anchor(bolt.Src, feet, nx, ny);
            var green = new Color(0.525f, 0.937f, 0.675f, 1f);
            Shot(hand, at + (hand - feet), green, new Color(0.93f, 0.96f, 0.94f, 1f), 8f, 3f, 18f, 6f, 12f, 160f);
        }

        void PlaceHourBolt(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var nx = 0.02f;
            var ny = 0.62f;
            var slot = FindSlot(bolt.Src);
            if (slot != null && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1 && slot.Pose == "Hour Bolt")
                {
                    nx = 0.46f;
                    ny = 0.60f;
                }
            }
            var hand = Anchor(bolt.Src, feet, nx, ny);
            var tip = at + (hand - feet);
            var gold = new Color(0.92f, 0.76f, 0.28f, 1f);
            var pale = new Color(0.86f, 0.78f, 1f, 1f);
            Shot(hand, tip, gold, pale, 6f, 3f, 10f, 10f, 16f, 140f);
            PlaceRing(tip, 42f, pale, 1f, false);
        }

        void PlaceTideBolt(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var nx = 0.08f;
            var ny = 0.58f;
            var slot = FindSlot(bolt.Src);
            if (slot != null && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1 && slot.Pose == "Tide Bolt")
                {
                    nx = 0.50f;
                    ny = 0.55f;
                }
            }
            var hand = Anchor(bolt.Src, feet, nx, ny);
            var tip = at + (hand - feet);
            var tide = new Color(0.13f, 0.83f, 0.93f, 1f);
            var pale = new Color(0.85f, 0.97f, 1f, 1f);
            Shot(hand, tip, tide, pale, 7f, 3f, 12f, 10f, 16f, 150f);
            PlaceRing(tip, 36f, pale, 1f, false);
        }

        void PlaceFlask(ArenaBolt bolt, Vector2 at, float width, float height)
        {
            var feet = OnLane(bolt.Src.X, width, height);
            var nx = 0.28f;
            var ny = 0.55f;
            var slot = FindSlot(bolt.Src);
            if (slot != null && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1 && slot.Pose == "Chemist Vial")
                {
                    nx = 0.55f;
                    ny = 0.62f;
                }
            }
            var hand = Anchor(bolt.Src, feet, nx, ny);
            var tip = at + (hand - feet);
            var gold = new Color(0.99f, 0.90f, 0.54f, 1f);
            var rose = new Color(0.93f, 0.28f, 0.72f, 1f);
            Shot(hand, tip, rose, gold, 6f, 3f, 12f, 12f, 18f, 140f);
            PlaceRing(tip, 34f, gold, 1f, false);
        }

        void Shot(Vector2 hand, Vector2 tip, Color color, Color hot, float thick, float core, float tipW, float tipH, float handSize, float flashLen)
        {
            var delta = tip - hand;
            var len = delta.magnitude;
            var ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var mid = (hand + tip) * 0.5f;
            if (len > 12f)
            {
                Place(_bolt, mid, new Vector2(len, thick), ang, color, 0.9f, false);
                Place(_lance, mid, new Vector2(len, core), ang, hot, 1f, false);
            }
            Place(_bolt, tip, new Vector2(tipW, tipH), ang, hot, 1f, false);
            Place(_ember, tip, new Vector2(tipH * 0.7f, tipH * 0.7f), 0f, color, 1f, false);
            Place(_ember, hand, new Vector2(handSize, handSize), 0f, color, 1f, false);
            var flash = Mathf.Clamp01(1f - len / flashLen);
            if (flash > 0.02f)
                Place(_burst, hand, new Vector2(Mathf.Lerp(40f, 160f, flash), Mathf.Lerp(40f, 160f, flash)), 0f, color, 0.45f + 0.4f * flash, false);
        }

        Vector2 Hand(ArenaBody body, Vector2 feet, string pose, float nx, float ny, float n1x, float n1y, float n2x, float n2y)
        {
            var slot = FindSlot(body);
            if (slot != null && slot.Pose == pose && slot.Clip != null && slot.Clip.Length > 0 && slot.ClipT >= 0f)
            {
                var u = slot.ClipDur > 0.01f ? slot.ClipT / slot.ClipDur : 1f;
                var frame = Mathf.Clamp(Mathf.FloorToInt(u * slot.Clip.Length), 0, slot.Clip.Length - 1);
                if (frame == 1)
                {
                    nx = n1x;
                    ny = n1y;
                }
                else if (frame == 2)
                {
                    nx = n2x;
                    ny = n2y;
                }
            }
            return Anchor(body, feet, nx, ny);
        }

        Vector2 Anchor(ArenaBody body, Vector2 feet, float nx, float ny)
        {
            var slot = FindSlot(body);
            if (slot == null || slot.Rect == null)
                return feet + new Vector2(0f, 48f);
            var face = slot.Rect.localScale.x >= 0f ? 1f : -1f;
            return feet + new Vector2(face * slot.Rect.sizeDelta.x * nx, slot.Rect.sizeDelta.y * ny);
        }

        Slot FindSlot(ArenaBody body)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Body == body)
                    return _slots[i];
            }
            return null;
        }

        void PlaceRing(Vector2 at, float diameter, Color color, float alpha, bool behind)
        {
            var wide = new Vector2(diameter, diameter * 0.42f);
            Place(_ringBack, at, wide, 0f, color, alpha * 0.85f, true);
            Place(_ringFront, at, wide, 0f, color, alpha, false);
            if (behind && _sigilN >= 2)
                _sigils[_sigilN - 2].Rect.SetSiblingIndex(0);
        }

        void Place(Material mat, Vector2 at, Vector2 size, float angle, Color color, float alpha, bool behind)
        {
            if (mat == null || _view.Glow == null)
                return;
            var node = TakeSigil();
            node.Image.sprite = _view.Glow;
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
                go.transform.SetParent(_view.StagePlate, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _sigils.Add(new Sigil { Rect = go.GetComponent<RectTransform>(), Image = image });
            }
            var node = _sigils[_sigilN];
            _sigilN++;
            node.Image.enabled = true;
            return node;
        }

        static Color KitColor(ArenaBody body)
        {
            var hex = LaneRoster.ById(body != null ? body.Id : "").Color;
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
        }

        static Color Hex(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return Color.white;
        }

        int Alive()
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
            if (_voice == null || string.IsNullOrEmpty(name))
                return;
            if (!_sfx.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name);
                _sfx[name] = clip;
            }
            if (clip != null)
                _voice.PlayOneShot(clip, gain);
        }
    }
}
