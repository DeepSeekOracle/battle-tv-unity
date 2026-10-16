using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Fight
{
    public sealed class DuelView : MonoBehaviour
    {
        public const float StageWidth = 2048f;
        public const float StageHeight = 1152f;
        const float Band = 720f / 1152f;
        const float Feet = (216f + 0.2f * 720f) / 1152f;

        [SerializeField] RectTransform plate;
        [SerializeField] RectTransform valeBody;
        [SerializeField] RectTransform bruteBody;
        [SerializeField] LaneHud valeHud;
        [SerializeField] LaneHud bruteHud;
        [SerializeField] Text clock;
        [SerializeField] Text callout;
        [SerializeField] Text result;
        [SerializeField] Sprite[] valeToss;
        [SerializeField] Sprite[] valeBash;
        [SerializeField] Sprite[] valePunch;
        [SerializeField] Sprite[] valeLine;
        [SerializeField] Sprite[] valePlant;
        [SerializeField] Image valeDoor;
        [SerializeField] Image valeRingBack;
        [SerializeField] Image valeRing;
        [SerializeField] Image hitSpark;
        [SerializeField] Sprite valeOpen;
        [SerializeField] Sprite[] bruteSweep;
        [SerializeField] Sprite[] brutePound;
        [SerializeField] Sprite[] bruteWard;
        [SerializeField] Sprite[] bruteClose;
        [SerializeField] Sprite bruteIdle;
        [SerializeField] Image poundLine;
        [SerializeField] Image gripRocks;

        const float TossTime = 0.5f;
        const float BashTime = 0.4f;
        const float PunchTime = 0.34f;
        const float LineTime = 0.5f;
        const float PlantTime = 0.4f;
        const float TossPivot = 0.3f;
        const float BashPivot = 0.39f;
        const float PunchPivot = 0.37f;
        const float LinePivot = 0.54f;
        const float PlantPivot = 0.56f;

        Body _vale;
        Body _brute;
        System.Random _rng;
        double _time;
        bool _over;
        int _appliedW = -1;
        int _appliedH = -1;
        string _lastLine = "";
        float _hitStop;
        float _sparkLife;
        string _sparkOn = "";
        bool _sparkCrit;
        float _valeRecoil;
        float _bruteRecoil;
        float _valeFlash;
        float _bruteFlash;
        bool _finishSaid;
        string _heardVale = "";
        string _heardBrute = "";
        AudioSource _voice;
        readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>(16);
        readonly List<Blow> _inbox = new List<Blow>(8);
        readonly List<Queued> _queue = new List<Queued>(8);
        readonly List<Floater> _floaters = new List<Floater>(8);
        readonly List<Text> _pool = new List<Text>(8);
        Sprite[] _clip;
        float _clipT = -1f;
        float _clipDur = 0.4f;
        float _clipPivot = 0.5f;
        Sprite[] _bruteClip;
        float _bruteClipT = -1f;
        float _bruteClipDur = 0.55f;
        float _prevSwing;
        float _prevAtk;
        float _prevBruteSwing;
        float _prevBruteAtk;
        float _lineT = -1f;
        float _wardPulse;
        bool _wardHold;
        Sprite _valeIdle;
        const float PoundLineTime = 0.62f;

        void Start()
        {
            SideDuel.TakeBlows(_inbox);
            _inbox.Clear();
            SideDuel.ResetLane();
            _vale = SideDuel.Vale();
            _brute = SideDuel.Brute();
            _rng = new System.Random();
            if (valeBody != null)
            {
                var planted = valeBody.GetComponent<Image>();
                if (planted != null)
                    _valeIdle = planted.sprite;
            }
            if (bruteIdle == null && bruteBody != null)
            {
                var planted = bruteBody.GetComponent<Image>();
                if (planted != null)
                    bruteIdle = planted.sprite;
            }
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 0f;
            Paint(0f);
            if (BattleTv.Menu.IntroMusic.Current != null)
                BattleTv.Menu.IntroMusic.Current.SetMuted(true);
            Play("fight", 0.5f);
        }

        public void BackToMenu()
        {
            if (BattleTv.Menu.IntroMusic.Current != null)
                BattleTv.Menu.IntroMusic.Current.SetMuted(false);
            SceneManager.LoadScene("GameMenu");
        }

        void Update()
        {
            float frameDt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            bool frozen = _hitStop > 0f;
            if (frozen)
                _hitStop = Mathf.Max(0f, _hitStop - frameDt);
            else
            {
                if (_clipT >= 0f)
                {
                    _clipT += frameDt;
                    if (_clipT >= _clipDur)
                        _clipT = -1f;
                }
                if (_bruteClipT >= 0f)
                {
                    _bruteClipT += frameDt;
                    if (_bruteClipT >= _bruteClipDur)
                        _bruteClipT = -1f;
                }
                if (_lineT >= 0f)
                    _lineT += frameDt;
            }
            _wardPulse += frameDt;
            Fit();
            if (!frozen && !_over && _vale != null && _brute != null)
            {
                double dt = frameDt;
                _time += dt;
                SideDuel.Step(_vale, _brute, dt, _rng, _time);
                float atk = (float)_vale.AtkCd;
                bool swung = _vale.Callout == "Punch" || _vale.Callout == "Shield Bash";
                if (atk > _prevAtk + 0.05f && swung)
                    BeginValeClip(_vale.Callout);
                else if (_vale.Swing > _prevSwing && _vale.Swing > 0.18f && _clipT < 0f)
                    BeginValeClip(_vale.Callout);
                _prevAtk = atk;
                _prevSwing = (float)_vale.Swing;
                float batk = (float)_brute.AtkCd;
                if (batk > _prevBruteAtk + 0.05f && _brute.Callout == "Heavy Sweep")
                    ArmBrute(bruteSweep, 1.25f);
                else if (_brute.Swing > _prevBruteSwing + 0.02f && _brute.Swing > 0.18f)
                {
                    if (_brute.Callout == "Brute Pound")
                    {
                        ArmBrute(brutePound, 0.7f);
                        _lineT = 0f;
                    }
                    else if (_brute.Callout == "Brute Ward")
                        _wardHold = true;
                    else if (_brute.Callout == "Close")
                        ArmBrute(bruteClose, 0.4f);
                }
                if (_brute.Shield <= 0)
                    _wardHold = false;
                _prevBruteAtk = batk;
                _prevBruteSwing = (float)_brute.Swing;
                CollectBlows();
                HearMove(_vale, ref _heardVale);
                HearMove(_brute, ref _heardBrute);
                if (!_vale.Alive || !_brute.Alive)
                {
                    _over = true;
                    string winner = _vale.Alive ? _vale.Name : _brute.Name;
                    if (result != null)
                        result.text = winner + " takes the lane";
                    Play("win", 0.55f);
                }
                else if (_time >= SideDuel.HardStop)
                {
                    _over = true;
                    if (result != null)
                        result.text = "Neither side fell. The lane is a draw.";
                }
            }
            Paint(frozen ? 0f : frameDt);
        }

        void Paint(float dt)
        {
            if (_vale == null || _brute == null || plate == null)
                return;
            Place(_vale, valeBody);
            Place(_brute, bruteBody);
            KeepShouldersApart();
            if (dt > 0f)
                ReleaseBlows(dt);
            ApplyWeight();
            CloseThePunch();
            PlaceDoor();
            PlaceRing();
            PlacePoundLine();
            PlaceGrip();
            PlaceSpark();
            TickFloaters(dt);
            bool fists = _vale.Range <= 110.0 / 32.0 && _brute.Range <= 110.0 / 32.0;
            if (valeHud != null)
                valeHud.Show(_vale, fists);
            if (bruteHud != null)
                bruteHud.Show(_brute, fists);
            if (clock != null)
            {
                int secs = Mathf.FloorToInt((float)_time);
                string face = (secs / 60) + ":" + (secs % 60).ToString("00");
                if (_time >= SideDuel.SuddenAt)
                {
                    int pct = (int)System.Math.Round((SideDuel.SuddenScale(_time) - 1) * 100);
                    face += "  +" + pct;
                }
                clock.text = face;
                clock.color = _time >= SideDuel.SuddenAt
                    ? new Color(0.98f, 0.75f, 0.14f, 1f)
                    : new Color(0.965f, 0.906f, 0.757f, 1f);
            }
            if (callout == null)
                return;
            string fresh = null;
            if (_brute.Swing > _vale.Swing && !string.IsNullOrEmpty(_brute.Callout))
                fresh = _brute.Name + "  " + _brute.Callout;
            else if (_vale.Swing > 0.02f && !string.IsNullOrEmpty(_vale.Callout))
                fresh = _vale.Name + "  " + _vale.Callout;
            if (!string.IsNullOrEmpty(fresh))
                _lastLine = fresh;
            callout.text = _lastLine;
        }

        void Place(Body body, RectTransform rect)
        {
            if (rect == null)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            bool playing = body.Name == "Vale" && _clipT >= 0f && _clip != null && _clip.Length > 0;
            bool brutePlaying = body.Name == "Brute" && _bruteClipT >= 0f && _bruteClip != null && _bruteClip.Length > 0;
            var image = rect.GetComponent<Image>();
            if (image != null && body.Name == "Vale")
            {
                if (playing)
                {
                    int frame = Mathf.Clamp(Mathf.FloorToInt(_clipT / _clipDur * _clip.Length), 0, _clip.Length - 1);
                    if (_sparkLife > 0.01f && _sparkOn == "Brute" && (_vale.Callout == "Punch" || _vale.Callout == "Shield Bash"))
                        frame = Mathf.Clamp(Mathf.FloorToInt(0.58f * _clip.Length), 0, _clip.Length - 1);
                    if (_clip[frame] != null)
                        image.sprite = _clip[frame];
                }
                else if (_valeIdle != null)
                    image.sprite = _valeIdle;
            }
            else if (image != null && body.Name == "Brute")
            {
                if (brutePlaying)
                {
                    int frame = Mathf.Clamp(Mathf.FloorToInt(_bruteClipT / _bruteClipDur * _bruteClip.Length), 0, _bruteClip.Length - 1);
                    if (_brute.Callout == "Heavy Sweep")
                    {
                        // Hold the club up, then hold the bonk. Equal slices were too fast to read.
                        float u = _bruteClipDur > 0.01f ? _bruteClipT / _bruteClipDur : 1f;
                        frame = u < 0.52f ? 0 : _bruteClip.Length - 1;
                    }
                    if (_sparkLife > 0.01f && _sparkOn == "Vale" && (_brute.Callout == "Heavy Sweep" || _brute.Callout == "Brute Pound"))
                        frame = _bruteClip.Length - 1;
                    if (_bruteClip[frame] != null)
                        image.sprite = _bruteClip[frame];
                }
                else if (_wardHold && bruteWard != null && bruteWard.Length > 0)
                {
                    int frame = ((int)(_wardPulse / 0.28f)) % bruteWard.Length;
                    if (bruteWard[frame] != null)
                        image.sprite = bruteWard[frame];
                }
                else if (bruteIdle != null)
                    image.sprite = bruteIdle;
            }
            float x = (float)(body.X / SideDuel.Lane) * width - width * 0.5f;
            float feet = -height * 0.5f + height * Feet;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            float pivotX = playing ? _clipPivot : 0.5f;
            if (body.Name == "Brute" && image != null && image.sprite != null && image.sprite.rect.width > 1f)
                pivotX = image.sprite.pivot.x / image.sprite.rect.width;
            rect.pivot = new Vector2(pivotX, 0f);
            float spriteH = height * Band * (body.Name == "Brute" ? 0.56f : 0.5f);
            float aspect = 0.7f;
            if (image != null && image.sprite != null)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet);
            float flash = body.Name == "Vale" ? _valeFlash : _bruteFlash;
            float recoil = body.Name == "Vale" ? _valeRecoil : _bruteRecoil;
            if (image != null)
                image.color = Color.Lerp(Color.white, new Color(1f, 0.94f, 0.82f), Mathf.Clamp01(flash));
            float give = recoil * 0.11f;
            // Vale's paintings face right, with the kite on her left forearm.
            // Brute's painting faces left. A negative scale turns the same frames
            // toward the other fighter. The mirror keeps the kite on that painted arm.
            float face = 1f;
            if (body.Name == "Vale" && _brute != null)
                face = body.X <= _brute.X ? 1f : -1f;
            else if (body.Name == "Brute" && _vale != null)
                face = body.X >= _vale.X ? 1f : -1f;
            rect.localScale = new Vector3(face * (1f + give), 1f - give, 1f);
        }

        static bool ValeOnLeft(RectTransform vale, RectTransform brute)
        {
            return vale.anchoredPosition.x <= brute.anchoredPosition.x;
        }

        static float Front(RectTransform rect, float home)
        {
            // Vale's paintings lead on the right of the pivot. Brute's lead on the left.
            // After a mirror, that same edge is still the nose or the club.
            float reachFrac = home > 0f ? (1f - rect.pivot.x) : rect.pivot.x;
            float face = (rect.localScale.x < 0f ? -1f : 1f) * home;
            return rect.anchoredPosition.x + face * reachFrac * SpanX(rect);
        }

        void KeepShouldersApart()
        {
            if (valeBody == null || bruteBody == null)
                return;
            bool valeOnLeft = ValeOnLeft(valeBody, bruteBody);
            float valeEdge = Front(valeBody, 1f);
            float bruteEdge = Front(bruteBody, -1f);
            float air = valeOnLeft ? bruteEdge - valeEdge : valeEdge - bruteEdge;
            // Negative air is the fist crossing the other chest. Stop the bodies before the torsos occupy the same space.
            const float bury = -18f;
            if (air >= bury)
                return;
            float push = (bury - air) * 0.5f;
            valeBody.anchoredPosition += new Vector2(valeOnLeft ? -push : push, 0f);
            bruteBody.anchoredPosition += new Vector2(valeOnLeft ? push : -push, 0f);
        }

        void ApplyWeight()
        {
            Shove(valeBody, _valeRecoil, bruteBody);
            Shove(bruteBody, _bruteRecoil, valeBody);
        }

        void CloseThePunch()
        {
            if (valeBody == null || bruteBody == null || _vale == null)
                return;
            if (_sparkLife <= 0.01f || _sparkOn != "Brute")
                return;
            if (_vale.Callout != "Punch" && _vale.Callout != "Shield Bash")
                return;
            bool valeOnLeft = ValeOnLeft(valeBody, bruteBody);
            float valeEdge = Front(valeBody, 1f);
            float bruteEdge = Front(bruteBody, -1f);
            float air = valeOnLeft ? bruteEdge - valeEdge : valeEdge - bruteEdge;
            // The live range connects before the painted fist reaches his chest.
            // Pull only the punch pose, and only until the fist crosses his front.
            const float want = -14f;
            if (air <= want)
                return;
            float pull = Mathf.Min(air - want, SpanX(valeBody) * 0.85f);
            valeBody.anchoredPosition += new Vector2(valeOnLeft ? pull : -pull, 0f);
        }

        static float SpanX(RectTransform rect)
        {
            return rect == null ? 0f : Mathf.Abs(rect.sizeDelta.x * rect.localScale.x);
        }

        static float SpanY(RectTransform rect)
        {
            return rect == null ? 0f : Mathf.Abs(rect.sizeDelta.y * rect.localScale.y);
        }

        static void Shove(RectTransform rect, float recoil, RectTransform other)
        {
            if (rect == null || other == null || recoil <= 0f)
                return;
            float away = Mathf.Sign(rect.anchoredPosition.x - other.anchoredPosition.x);
            if (Mathf.Abs(away) < 0.01f)
                away = 1f;
            // View only. The sim position stays. A third of a body is enough to read as weight.
            rect.anchoredPosition += new Vector2(away * SpanX(rect) * 0.30f * recoil, 0f);
        }

        void PlaceDoor()
        {
            if (valeDoor == null)
                return;
            bool planting = _clipT >= 0f && _vale != null && _vale.Callout == "Aegis Wall";
            bool up = SideDuel.DoorT > 0 && !planting;
            valeDoor.enabled = up;
            if (!up || plate == null)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            float x = (float)(SideDuel.DoorX / SideDuel.Lane) * width - width * 0.5f;
            float feet = -height * 0.5f + height * Feet;
            var rect = valeDoor.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.02f);
            float spriteH = height * Band * 0.24f;
            float aspect = valeDoor.sprite != null ? valeDoor.sprite.rect.width / valeDoor.sprite.rect.height : 0.9f;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet - spriteH * 0.14f);
        }

        void PlaceRing()
        {
            bool up = _vale != null && _vale.ArmorBuffT > 0.05 && plate != null;
            if (valeRingBack != null)
                valeRingBack.enabled = up;
            if (valeRing != null)
                valeRing.enabled = up;
            if (!up || plate == null)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            float x = valeBody != null ? valeBody.anchoredPosition.x : (float)(_vale.X / SideDuel.Lane) * width - width * 0.5f;
            float feet = -height * 0.5f + height * Feet;
            // Wide and flat. The centre sits on her feet, so the top of the
            // ellipse is behind her calves and the bottom is on the sand in front.
            float span = height * Band * 0.92f;
            float thick = span / 3.2f;
            float life = Mathf.Clamp01((float)(_vale.ArmorBuffT / 8.0));
            float fade = Mathf.Clamp01((float)(_vale.ArmorBuffT / 0.45));
            var tint = new Color(1f, 1f, 1f, fade * (0.70f + 0.30f * life));
            Seat(valeRingBack, x, feet, span, thick, tint);
            Seat(valeRing, x, feet, span, thick, tint);
        }

        static void Seat(Image image, float x, float y, float span, float thick, Color tint)
        {
            if (image == null)
                return;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(span, thick);
            rect.anchoredPosition = new Vector2(x, y);
            image.color = tint;
        }

        void CollectBlows()
        {
            _inbox.Clear();
            SideDuel.TakeBlows(_inbox);
            bool valeStrike = _clipT >= 0f && (_vale.Callout == "Punch" || _vale.Callout == "Shield Bash");
            bool bruteStrike = _bruteClipT >= 0f && (_brute.Callout == "Heavy Sweep" || _brute.Callout == "Brute Pound");
            for (int i = 0; i < _inbox.Count; i++)
            {
                Blow blow = _inbox[i];
                var queued = new Queued { Blow = blow, Wait = 0.07f, ClipAt = -1f };
                if (blow.Tone == "block" || blow.Tone == "ward")
                    queued.Wait = 0f;
                else if (blow.Target == "Brute" && valeStrike)
                {
                    queued.Wait = -1f;
                    queued.ClipAt = _clipDur * 0.58f;
                }
                else if (blow.Target == "Vale" && bruteStrike)
                {
                    queued.Wait = -1f;
                    queued.ClipAt = _bruteClipDur * 0.72f;
                    queued.OnBrute = true;
                }
                _queue.Add(queued);
            }
        }

        void ReleaseBlows(float dt)
        {
            float stop = 0f;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                Queued queued = _queue[i];
                if (queued.ClipAt >= 0f)
                {
                    float clock = queued.OnBrute ? _bruteClipT : _clipT;
                    if (clock < queued.ClipAt)
                        continue;
                }
                else
                {
                    queued.Wait -= dt;
                    if (queued.Wait > 0f)
                    {
                        _queue[i] = queued;
                        continue;
                    }
                }
                ShowBlow(queued.Blow);
                if (queued.Blow.Tone == "down")
                    stop = Mathf.Max(stop, 0.12f);
                else if (queued.Blow.Tone == "hit" || queued.Blow.Tone == "crit")
                    stop = Mathf.Max(stop, 0.05f);
                else if (queued.Blow.Tone == "ward" || queued.Blow.Tone == "block")
                    stop = Mathf.Max(stop, 0.04f);
                _queue.RemoveAt(i);
            }
            if (stop > _hitStop)
                _hitStop = stop;
        }

        void ShowBlow(Blow blow)
        {
            RectTransform body = blow.Target == "Vale" ? valeBody : bruteBody;
            if (body == null)
                return;
            bool spark = blow.Tone == "hit" || blow.Tone == "crit" || blow.Tone == "down"
                || blow.Tone == "ward" || blow.Tone == "block";
            if (spark)
            {
                _sparkLife = 0.18f;
                _sparkOn = blow.Target;
                _sparkCrit = blow.Tone == "crit";
                float weight = blow.Tone == "crit" || blow.Tone == "down" ? 1.35f
                    : blow.Tone == "ward" || blow.Tone == "block" ? 0.4f
                    : 1f;
                if (blow.Target == "Vale")
                {
                    _valeRecoil = weight;
                    _valeFlash = 1f;
                }
                else
                {
                    _bruteRecoil = weight;
                    _bruteFlash = 1f;
                }
                Play(BlowSound(blow), blow.Tone == "ward" || blow.Tone == "block" ? 0.22f : 0.4f);
            }
            Color color = Color.white;
            int size = 15;
            if (blow.Tone == "crit")
            {
                color = new Color(0.984f, 0.749f, 0.141f);
                size = 18;
            }
            else if (blow.Tone == "block")
                color = new Color(0.580f, 0.639f, 0.722f);
            else if (blow.Tone == "ward")
                color = new Color(0.490f, 0.827f, 0.988f);
            else if (blow.Tone == "down")
            {
                color = new Color(0.973f, 0.443f, 0.443f);
                size = 18;
            }
            SpawnFloater(blow.Text, color, size, body, blow.Tone == "down" ? 36f : 8f);
            if (blow.Tone == "down" && !_finishSaid)
            {
                _finishSaid = true;
                SpawnFloater("FINISH", new Color(0.984f, 0.749f, 0.141f), 26, body, 78f);
            }
        }

        void PlaceSpark()
        {
            if (hitSpark == null)
                return;
            hitSpark.enabled = _sparkLife > 0.01f && !string.IsNullOrEmpty(_sparkOn);
            if (!hitSpark.enabled)
                return;
            RectTransform body = _sparkOn == "Vale" ? valeBody : bruteBody;
            RectTransform other = _sparkOn == "Vale" ? bruteBody : valeBody;
            if (body == null)
                return;
            float toward = other != null ? Mathf.Sign(other.anchoredPosition.x - body.anchoredPosition.x) : (_sparkOn == "Vale" ? 1f : -1f);
            if (Mathf.Abs(toward) < 0.01f)
                toward = 1f;
            float chestX = body.anchoredPosition.x + toward * SpanX(body) * 0.28f;
            float chestY = body.anchoredPosition.y + SpanY(body) * 0.60f;
            float span = SpanY(body) * (0.34f + 0.16f * (1f - Mathf.Clamp01(_sparkLife / 0.18f)));
            var rect = hitSpark.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(span, span);
            rect.anchoredPosition = new Vector2(chestX, chestY);
            hitSpark.color = _sparkCrit
                ? new Color(1f, 0.82f, 0.42f, Mathf.Clamp01(_sparkLife / 0.12f))
                : new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(_sparkLife / 0.12f));
            hitSpark.transform.SetAsLastSibling();
        }

        void TickFloaters(float dt)
        {
            if (dt > 0f)
            {
                _sparkLife = Mathf.Max(0f, _sparkLife - dt);
                _valeRecoil = Mathf.Max(0f, _valeRecoil - dt / 0.36f);
                _bruteRecoil = Mathf.Max(0f, _bruteRecoil - dt / 0.36f);
                _valeFlash = Mathf.Max(0f, _valeFlash - dt / 0.12f);
                _bruteFlash = Mathf.Max(0f, _bruteFlash - dt / 0.12f);
            }
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                Floater floater = _floaters[i];
                floater.Life -= dt;
                if (floater.Life <= 0f)
                {
                    if (floater.Label != null)
                        floater.Label.gameObject.SetActive(false);
                    _floaters.RemoveAt(i);
                    continue;
                }
                floater.Y += 42f * dt;
                if (floater.Label != null)
                {
                    floater.Label.rectTransform.anchoredPosition = new Vector2(floater.X, floater.Y);
                    Color color = floater.Label.color;
                    color.a = Mathf.Clamp01(floater.Life * 1.6f);
                    floater.Label.color = color;
                }
                _floaters[i] = floater;
            }
        }

        void SpawnFloater(string text, Color color, int size, RectTransform body, float lift)
        {
            Text label = NextFloater();
            label.fontSize = size;
            label.text = text;
            color.a = 1f;
            label.color = color;
            label.gameObject.SetActive(true);
            float x = body.anchoredPosition.x;
            float y = body.anchoredPosition.y + body.rect.height * 0.62f + lift;
            label.rectTransform.anchoredPosition = new Vector2(x, y);
            float life = size >= 26 ? 1.2f : size >= 18 ? 1f : 0.75f;
            _floaters.Add(new Floater { Label = label, Life = life, X = x, Y = y });
        }

        Text NextFloater()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                    return _pool[i];
            }
            var go = new GameObject("HitNumber", typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 40f);
            var label = go.GetComponent<Text>();
            label.font = clock != null && clock.font != null
                ? clock.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);
            _pool.Add(label);
            return label;
        }

        struct Queued
        {
            public Blow Blow;
            public float Wait;
            public float ClipAt;
            public bool OnBrute;
        }

        struct Floater
        {
            public Text Label;
            public float Life;
            public float X;
            public float Y;
        }

        void HearMove(Body body, ref string heard)
        {
            string now = body.Callout ?? "";
            if (now == heard)
                return;
            heard = now;
            if (now == "Aegis Wall")
                Play("vale_wall", 0.4f);
            else if (now == "Line Bulwark")
                Play("vale_line", 0.4f);
            else if (now == "Aegis Cover")
                Play("vale_cover", 0.4f);
            else if (now == "Brute Ward" || now == "Ward Field")
                Play("brute_ward", 0.4f);
            else if (now == "Brute Pound" || now == "Ground Pound")
                Play("brute_pound", 0.46f);
            else if (now == "Iron Grip")
                Play("cast", 0.4f);
            else if (now == "Close" && body.Name == "Brute")
                Play("brute_sweep", 0.28f);
        }

        string BlowSound(Blow blow)
        {
            if (blow.Tone == "down")
                return "kill";
            if (blow.Tone == "crit")
                return "big";
            bool valeSwung = blow.Target == "Brute" && _vale != null
                && (_vale.Callout == "Punch" || _vale.Callout == "Shield Bash");
            if (valeSwung)
                return "vale_cleave";
            bool bruteSwung = blow.Target == "Vale" && _brute != null && _brute.Callout == "Heavy Sweep";
            if (bruteSwung)
                return "brute_sweep";
            return "hit";
        }

        void Play(string name, float gain)
        {
            if (_voice == null || string.IsNullOrEmpty(name))
                return;
            if (!_sfx.TryGetValue(name, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("Sfx/" + name);
                _sfx[name] = clip;
            }
            if (clip == null)
                return;
            _voice.PlayOneShot(clip, gain);
        }

        void BeginValeClip(string callout)
        {
            if (callout == "Shield Bash")
                Arm(valeBash, BashTime, BashPivot);
            else if (callout == "Punch")
                Arm(valePunch, PunchTime, PunchPivot);
            else if (callout == "Aegis Cover")
                Arm(valeToss, TossTime, TossPivot);
            else if (callout == "Line Bulwark")
                Arm(valeLine, LineTime, LinePivot);
            else if (callout == "Aegis Wall")
                Arm(valePlant, PlantTime, PlantPivot);
        }

        void PlacePoundLine()
        {
            if (poundLine == null)
                return;
            bool up = _lineT >= 0f && _lineT < PoundLineTime && plate != null && bruteBody != null && valeBody != null;
            poundLine.enabled = up;
            if (!up)
                return;
            float u = Mathf.Clamp01(_lineT / PoundLineTime);
            float from = bruteBody.anchoredPosition.x;
            float to = valeBody.anchoredPosition.x;
            float dir = Mathf.Sign(to - from);
            if (Mathf.Abs(dir) < 0.01f)
                dir = -1f;
            float height = plate.rect.height;
            float feet = -height * 0.5f + height * Feet;
            float span = height * Band * Mathf.Lerp(0.42f, 0.92f, u);
            float thick = height * Band * 0.24f;
            var rect = poundLine.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.08f);
            rect.sizeDelta = new Vector2(span, thick);
            rect.anchoredPosition = new Vector2(Mathf.Lerp(from, to, u * 0.9f), feet);
            rect.localScale = new Vector3(dir, 1f, 1f);
            float fade = u < 0.12f ? u / 0.12f : Mathf.Clamp01((1f - u) / 0.28f);
            poundLine.color = new Color(1f, 1f, 1f, fade);
        }

        void PlaceGrip()
        {
            if (gripRocks == null)
                return;
            Body slowed = null;
            RectTransform feetBody = null;
            if (_vale != null && _vale.SlowT > 0.02)
            {
                slowed = _vale;
                feetBody = valeBody;
            }
            else if (_brute != null && _brute.SlowT > 0.02)
            {
                slowed = _brute;
                feetBody = bruteBody;
            }
            gripRocks.enabled = slowed != null && feetBody != null && plate != null;
            if (!gripRocks.enabled)
                return;
            float height = plate.rect.height;
            float span = height * Band * 0.20f;
            float aspect = gripRocks.sprite != null && gripRocks.sprite.rect.height > 1f
                ? gripRocks.sprite.rect.width / gripRocks.sprite.rect.height
                : 1.1f;
            var rect = gripRocks.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.06f);
            rect.sizeDelta = new Vector2(span * aspect, span);
            rect.anchoredPosition = new Vector2(feetBody.anchoredPosition.x, feetBody.anchoredPosition.y);
            rect.localScale = Vector3.one;
            gripRocks.color = new Color(1f, 1f, 1f, Mathf.Clamp01((float)(slowed.SlowT / 0.35)));
        }

        void ArmBrute(Sprite[] frames, float duration)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
                return;
            _bruteClip = frames;
            _bruteClipDur = duration;
            _bruteClipT = 0f;
        }

        void Arm(Sprite[] frames, float duration, float pivot)
        {
            if (frames == null || frames.Length == 0)
                return;
            _clip = frames;
            _clipDur = duration;
            _clipPivot = pivot;
            _clipT = 0f;
        }

        void Fit()
        {
            if (plate == null)
                return;
            int w = Screen.width;
            int h = Screen.height;
            if (w < 1 || h < 1 || (w == _appliedW && h == _appliedH))
                return;
            float aspect = StageWidth / StageHeight;
            float drawnW = w;
            float drawnH = w / aspect;
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
    }
}
