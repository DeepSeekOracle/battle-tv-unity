using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Fight
{
    public sealed class DuelView : MonoBehaviour
    {
        public const float StageWidth = 2048f;
        public const float StageHeight = 720f;

        [SerializeField] RectTransform plate;
        [SerializeField] RectTransform valeBody;
        [SerializeField] RectTransform bruteBody;
        [SerializeField] Image valeFill;
        [SerializeField] Image bruteFill;
        [SerializeField] Text valeHp;
        [SerializeField] Text bruteHp;
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
            Write(valeFill, valeHp, _vale);
            Write(bruteFill, bruteHp, _brute);
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
            float feet = -height * 0.5f + height * 0.2f;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            float spriteH = height * (body.Name == "Brute" ? 0.56f : 0.5f);
            var image = rect.GetComponent<Image>();
            float aspect = 0.7f;
            if (image != null && image.sprite != null)
                aspect = image.sprite.rect.width / image.sprite.rect.height;
            rect.sizeDelta = new Vector2(spriteH * aspect, spriteH);
            rect.anchoredPosition = new Vector2(x, feet);
        }

        static void Write(Image fill, Text label, Body body)
        {
            float ratio = body.Max <= 0 ? 0f : (float)(body.Hp / body.Max);
            if (fill != null)
            {
                var rect = fill.rectTransform;
                rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            if (label != null)
            {
                string plate = body.Shield > 0.5 ? "    plate " + Mathf.CeilToInt((float)body.Shield) : "";
                label.text = body.Name + "  " + Mathf.CeilToInt((float)body.Hp) + plate + "    " + body.Cards;
            }
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
