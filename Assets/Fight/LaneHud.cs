using UnityEngine;
using UnityEngine.UI;

namespace BattleTv.Fight
{
    /// <summary>
    /// Side-lane readout. Health and mana are the roster pools. Stamina is the
    /// swing breath between basics. Block is the plate, not a new meter. Dash is
    /// Close, the thirty-second step, and it stays held when both bodies are fists.
    /// </summary>
    public sealed class LaneHud : MonoBehaviour
    {
        [SerializeField] Image health;
        [SerializeField] Image mana;
        [SerializeField] Image stam;
        [SerializeField] Image block;
        [SerializeField] Image dash;
        [SerializeField] Text title;
        [SerializeField] Text cards;
        [SerializeField] Text healthNum;
        [SerializeField] Text manaNum;
        [SerializeField] Text blockNum;
        [SerializeField] Text dashNum;
        [SerializeField] bool fromRight;

        static readonly Color DashHeld = new Color(0.55f, 0.48f, 0.32f, 1f);

        public void Show(Body body, bool dashHeld)
        {
            if (body == null)
                return;
            ShowPools(body.Name, body.Cards, body.Hp, body.Max, body.Mana, body.ManaMax, body.AtkCd, body.Rate, body.Shield, body.StrideCd, dashHeld);
            if (title != null)
                title.text = string.IsNullOrEmpty(body.Cards)
                    ? "<size=18><b><color=#fff1c9>" + body.Name + "</color></b></size>"
                    : "<size=18><b><color=#fff1c9>" + body.Name + "</color></b></size>  <size=13><color=#c4b48a>" + body.Cards + "</color></size>";
        }

        public void ShowPools(string name, string cardsLine, double hp, double max, double manaPool, double manaMax, double atkCd, double rate, double shield, double strideCd, bool dashHeld)
        {
            Set(health, max <= 0 ? 0f : (float)(hp / max));
            Set(mana, manaMax <= 0 ? 0f : (float)(manaPool / manaMax));
            double swing = 1.0 / System.Math.Max(0.15, rate);
            float stamRatio = atkCd <= 0 ? 1f : Mathf.Clamp01((float)(1.0 - atkCd / swing));
            Set(stam, stamRatio);
            double cap = System.Math.Max(1, System.Math.Round(max * 0.4));
            Set(block, Mathf.Clamp01((float)(shield / cap)));
            float dashRatio = strideCd <= 0
                ? (manaPool >= SideDuel.CloseCost ? 1f : (float)(manaPool / SideDuel.CloseCost))
                : Mathf.Clamp01((float)(1.0 - strideCd / SideDuel.CloseCd));
            Set(dash, dashRatio);

            if (title != null)
                title.text = string.IsNullOrEmpty(cardsLine)
                    ? "<size=16><b><color=#fff1c9>" + name + "</color></b></size>"
                    : "<size=16><b><color=#fff1c9>" + name + "</color></b></size>  <size=11><color=#c4b48a>" + cardsLine + "</color></size>";
            if (cards != null)
                cards.text = "";
            if (healthNum != null)
                healthNum.text = Mathf.CeilToInt((float)hp).ToString();
            if (manaNum != null)
                manaNum.text = Mathf.CeilToInt((float)manaPool).ToString();
            if (blockNum != null)
                blockNum.text = shield > 0.5
                    ? Mathf.CeilToInt((float)shield).ToString()
                    : "BK";
            if (dashNum != null)
                dashNum.text = strideCd > 0.05
                    ? Mathf.CeilToInt((float)strideCd).ToString()
                    : "DA";
            if (block != null)
                block.color = Color.white;
            if (dash != null)
                dash.color = dashHeld && strideCd <= 0.05 ? DashHeld : Color.white;
        }

        public static LaneHud Create(Transform parent, string name, Vector2 min, Vector2 max, bool fromRight, Sprite bar, Sprite pip, Sprite healthFill, Sprite manaFill, Sprite stamFill, Sprite blockFill, Sprite dashFill)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = min;
            rootRect.anchorMax = max;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var plateImage = root.GetComponent<Image>();
            plateImage.color = new Color(0f, 0f, 0f, 0f);
            plateImage.raycastTarget = false;

            var host = root.transform;
            var title = Line(host, "Title", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.26f), TextAnchor.MiddleLeft, 15);
            title.supportRichText = true;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            float barL = 0.02f;
            float barR = 0.98f;
            var health = Rail(host, "Health", new Vector2(barL, 0.78f), new Vector2(barR, 0.98f), healthFill, bar);
            var healthNum = Line(host, "HealthNum", new Vector2(barL + 0.08f, 0.78f), new Vector2(barR - 0.02f, 0.98f), TextAnchor.MiddleRight, 12);
            var mana = Rail(host, "Mana", new Vector2(barL, 0.56f), new Vector2(barR, 0.74f), manaFill, bar);
            var manaNum = Line(host, "ManaNum", new Vector2(barL + 0.08f, 0.56f), new Vector2(barR - 0.02f, 0.74f), TextAnchor.MiddleRight, 12);
            manaNum.color = new Color(0.73f, 0.9f, 0.98f, 1f);
            float pipY0 = 0.30f;
            float pipY1 = 0.52f;
            float pipW = 0.2f;
            float pip0 = barL;
            float pip1 = barL + 0.22f;
            float pip2 = barL + 0.44f;
            var stam = Rail(host, "Stam", new Vector2(pip0, pipY0), new Vector2(pip0 + pipW, pipY1), stamFill, pip);
            var block = Rail(host, "Block", new Vector2(pip1, pipY0), new Vector2(pip1 + pipW, pipY1), blockFill, pip);
            var dash = Rail(host, "Dash", new Vector2(pip2, pipY0), new Vector2(pip2 + pipW, pipY1), dashFill, pip);
            var healthWord = Line(host, "HealthWord", new Vector2(barL + 0.02f, 0.78f), new Vector2(barL + 0.16f, 0.98f), TextAnchor.MiddleLeft, 11);
            healthWord.text = "HP";
            var manaWord = Line(host, "ManaWord", new Vector2(barL + 0.02f, 0.56f), new Vector2(barL + 0.16f, 0.74f), TextAnchor.MiddleLeft, 11);
            manaWord.text = "MP";
            var stamWord = Line(host, "StamWord", new Vector2(pip0, pipY0), new Vector2(pip0 + pipW, pipY1), TextAnchor.MiddleCenter, 11);
            stamWord.text = "ST";
            var blockNum = Line(host, "BlockNum", new Vector2(pip1, pipY0), new Vector2(pip1 + pipW, pipY1), TextAnchor.MiddleCenter, 11);
            blockNum.text = "BK";
            var dashNum = Line(host, "DashNum", new Vector2(pip2, pipY0), new Vector2(pip2 + pipW, pipY1), TextAnchor.MiddleCenter, 11);
            dashNum.text = "DA";

            var hud = root.AddComponent<LaneHud>();
            hud.health = health;
            hud.mana = mana;
            hud.stam = stam;
            hud.block = block;
            hud.dash = dash;
            hud.title = title;
            hud.healthNum = healthNum;
            hud.manaNum = manaNum;
            hud.blockNum = blockNum;
            hud.dashNum = dashNum;
            hud.fromRight = fromRight;
            return hud;
        }

        static Image Rail(Transform parent, string name, Vector2 min, Vector2 max, Sprite fillSprite, Sprite frame)
        {
            var track = new GameObject(name, typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = min;
            trackRect.anchorMax = max;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            var back = track.GetComponent<Image>();
            back.color = new Color(0.05f, 0.03f, 0.02f, 0.88f);
            back.raycastTarget = false;
            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(track.transform, false);
            var fill = fillObject.GetComponent<Image>();
            fill.sprite = fillSprite;
            fill.color = Color.white;
            fill.raycastTarget = false;
            var coverObject = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            coverObject.transform.SetParent(track.transform, false);
            var coverRect = coverObject.GetComponent<RectTransform>();
            coverRect.anchorMin = Vector2.zero;
            coverRect.anchorMax = Vector2.one;
            coverRect.offsetMin = Vector2.zero;
            coverRect.offsetMax = Vector2.zero;
            var cover = coverObject.GetComponent<Image>();
            cover.sprite = frame;
            cover.type = Image.Type.Sliced;
            cover.color = Color.white;
            cover.raycastTarget = false;
            return fill;
        }

        static Text Line(Transform parent, string name, Vector2 min, Vector2 max, TextAnchor align, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.fontSize = size;
            text.alignment = align;
            text.color = new Color(0xf6 / 255f, 0xe7 / 255f, 0xc1 / 255f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.92f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        void Set(Image fill, float ratio)
        {
            if (fill == null)
                return;
            ratio = Mathf.Clamp01(ratio);
            var rect = fill.rectTransform;
            if (fromRight)
            {
                rect.anchorMin = new Vector2(1f - ratio, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(ratio, 1f);
            }
            rect.offsetMin = new Vector2(10f, 4f);
            rect.offsetMax = new Vector2(-10f, -4f);
        }
    }
}
