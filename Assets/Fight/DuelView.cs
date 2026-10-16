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
        [SerializeField] Image valeRing;
        [SerializeField] Sprite valeOpen;

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
        Sprite[] _clip;
        float _clipT = -1f;
        float _clipDur = 0.4f;
        float _clipPivot = 0.5f;
        float _prevSwing;
        Sprite _valeIdle;

        void Start()
        {
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
            Paint();
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene("GameMenu");
        }

        void Update()
        {
            float frameDt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_clipT >= 0f)
            {
                _clipT += frameDt;
                if (_clipT >= _clipDur)
                    _clipT = -1f;
            }
            Fit();
            if (!_over && _vale != null && _brute != null)
            {
                double dt = frameDt;
                _time += dt;
                SideDuel.Step(_vale, _brute, dt, _rng, _time);
                if (_vale.Swing > _prevSwing && _vale.Swing > 0.18f && _clipT < 0f)
                    BeginValeClip(_vale.Callout);
                _prevSwing = (float)_vale.Swing;
                if (!_vale.Alive || !_brute.Alive)
                {
                    _over = true;
                    string winner = _vale.Alive ? _vale.Name : _brute.Name;
                    if (result != null)
                        result.text = winner + " takes the lane";
                }
                else if (_time >= SideDuel.HardStop)
                {
                    _over = true;
                    if (result != null)
                        result.text = "Neither side fell. The lane is a draw.";
                }
            }
            Paint();
        }

        void Paint()
        {
            if (_vale == null || _brute == null || plate == null)
                return;
            Place(_vale, valeBody, 1);
            Place(_brute, bruteBody, -1);
            PlaceDoor();
            PlaceRing();
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

        void Place(Body body, RectTransform rect, int facing)
        {
            if (rect == null)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            bool playing = body.Name == "Vale" && _clipT >= 0f && _clip != null && _clip.Length > 0;
            bool open = body.Name == "Vale" && !playing && SideDuel.DoorT > 0 && valeOpen != null;
            var image = rect.GetComponent<Image>();
            if (image != null && body.Name == "Vale")
            {
                if (playing)
                {
                    int frame = Mathf.Clamp(Mathf.FloorToInt(_clipT / _clipDur * _clip.Length), 0, _clip.Length - 1);
                    if (_clip[frame] != null)
                        image.sprite = _clip[frame];
                }
                else if (open && valeOpen != null)
                    image.sprite = valeOpen;
                else if (_valeIdle != null)
                    image.sprite = _valeIdle;
            }
            float x = (float)(body.X / SideDuel.Lane) * width - width * 0.5f;
            if (body.Swing > 0.08f && !playing)
                x += facing * width * 0.012f;
            float feet = -height * 0.5f + height * Feet;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(playing ? _clipPivot : (open ? TossPivot : 0.5f), 0f);
            float spriteH = height * Band * (body.Name == "Brute" ? 0.56f : 0.5f);
            float aspect = 0.7f;
            if (image != null && image.sprite != null)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet);
        }

        void PlaceDoor()
        {
            if (valeDoor == null)
                return;
            bool up = SideDuel.DoorT > 0;
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
            rect.pivot = new Vector2(0.5f, 0.08f);
            float spriteH = height * Band * 0.42f;
            float aspect = valeDoor.sprite != null ? valeDoor.sprite.rect.width / valeDoor.sprite.rect.height : 0.9f;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet);
        }

        void PlaceRing()
        {
            if (valeRing == null)
                return;
            bool up = _vale != null && _vale.ArmorBuffT > 0.05 && plate != null;
            valeRing.enabled = up;
            if (!up)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            float x = (float)(_vale.X / SideDuel.Lane) * width - width * 0.5f;
            float feet = -height * 0.5f + height * Feet;
            var rect = valeRing.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float span = height * Band * 0.9f;
            float aspect = valeRing.sprite != null && valeRing.sprite.rect.height > 1f
                ? valeRing.sprite.rect.width / valeRing.sprite.rect.height
                : 1.8f;
            rect.sizeDelta = new Vector2(span, span / aspect);
            rect.anchoredPosition = new Vector2(x, feet + height * 0.02f);
            float life = Mathf.Clamp01((float)(_vale.ArmorBuffT / 8.0));
            valeRing.color = new Color(1f, 1f, 1f, 0.72f + 0.28f * life);
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
