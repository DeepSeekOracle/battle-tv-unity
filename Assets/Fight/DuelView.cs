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

        Body _vale;
        Body _brute;
        System.Random _rng;
        double _time;
        bool _over;
        int _appliedW = -1;
        int _appliedH = -1;

        void Start()
        {
            _vale = SideDuel.Vale();
            _brute = SideDuel.Brute();
            _rng = new System.Random();
            Paint();
        }

        public void BackToMenu()
        {
            SceneManager.LoadScene("GameMenu");
        }

        void Update()
        {
            Fit();
            if (!_over && _vale != null && _brute != null)
            {
                double dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                _time += dt;
                SideDuel.Step(_vale, _brute, dt, _rng, _time);
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
            bool fists = _vale.Range <= 110.0 / 32.0 && _brute.Range <= 110.0 / 32.0;
            if (valeHud != null)
                valeHud.Show(_vale, fists);
            if (bruteHud != null)
                bruteHud.Show(_brute, fists);
            if (clock != null)
            {
                int secs = Mathf.FloorToInt((float)_time);
                clock.text = (secs / 60) + ":" + (secs % 60).ToString("00");
                clock.color = _time >= SideDuel.SuddenAt
                    ? new Color(0.98f, 0.75f, 0.14f, 1f)
                    : new Color(0.965f, 0.906f, 0.757f, 1f);
            }
            if (callout == null)
                return;
            string line = "";
            if (!string.IsNullOrEmpty(_vale.Callout))
                line = _vale.Name + "  " + _vale.Callout;
            if (!string.IsNullOrEmpty(_brute.Callout))
                line = (line.Length == 0 ? "" : line + "    ") + _brute.Name + "  " + _brute.Callout;
            if (_time >= SideDuel.SuddenAt)
            {
                int pct = (int)System.Math.Round((SideDuel.SuddenScale(_time) - 1) * 100);
                string sudden = "Sudden death  +" + pct + "%";
                line = line.Length == 0 ? sudden : sudden + "    " + line;
            }
            callout.text = line;
        }

        void Place(Body body, RectTransform rect, int facing)
        {
            if (rect == null)
                return;
            float width = plate.rect.width;
            float height = plate.rect.height;
            if (width < 2f || height < 2f)
                return;
            float x = (float)(body.X / SideDuel.Lane) * width - width * 0.5f;
            if (body.Swing > 0.08)
                x += facing * width * 0.012f;
            float feet = -height * 0.5f + height * Feet;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            float spriteH = height * Band * (body.Name == "Brute" ? 0.56f : 0.5f);
            var image = rect.GetComponent<Image>();
            float aspect = 0.7f;
            if (image != null && image.sprite != null)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet);
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
            if (drawnH > h)
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
