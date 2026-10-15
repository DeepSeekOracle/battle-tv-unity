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

        static readonly Color BlockLit = new Color(0.84f, 0.87f, 0.91f, 1f);
        static readonly Color BlockDim = new Color(0.45f, 0.48f, 0.52f, 1f);
        static readonly Color DashReady = new Color(0.98f, 0.75f, 0.14f, 1f);
        static readonly Color DashHeld = new Color(0.55f, 0.42f, 0.16f, 1f);

        public void Show(Body body, bool dashHeld)
        {
            if (body == null)
                return;
            Set(health, body.Max <= 0 ? 0f : (float)(body.Hp / body.Max));
            Set(mana, body.ManaMax <= 0 ? 0f : (float)(body.Mana / body.ManaMax));
            double swing = 1.0 / System.Math.Max(0.15, body.Rate);
            float stamRatio = body.AtkCd <= 0 ? 1f : Mathf.Clamp01((float)(1.0 - body.AtkCd / swing));
            Set(stam, stamRatio);
            double cap = System.Math.Max(1, System.Math.Round(body.Max * 0.4));
            Set(block, Mathf.Clamp01((float)(body.Shield / cap)));
            float dashRatio = body.StrideCd <= 0
                ? (body.Mana >= SideDuel.CloseCost ? 1f : (float)(body.Mana / SideDuel.CloseCost))
                : Mathf.Clamp01((float)(1.0 - body.StrideCd / SideDuel.CloseCd));
            Set(dash, dashRatio);

            if (title != null)
                title.text = body.Name;
            if (cards != null)
                cards.text = body.Cards;
            if (healthNum != null)
                healthNum.text = Mathf.CeilToInt((float)body.Hp).ToString();
            if (manaNum != null)
                manaNum.text = Mathf.CeilToInt((float)body.Mana).ToString();
            if (blockNum != null)
                blockNum.text = body.Shield > 0.5
                    ? "B " + Mathf.CeilToInt((float)body.Shield)
                    : "BLOCK";
            if (dashNum != null)
                dashNum.text = body.StrideCd > 0.05
                    ? Mathf.CeilToInt((float)body.StrideCd).ToString()
                    : "DASH";
            if (block != null)
                block.color = body.Shield > 0.5 || body.Callout == "block" ? BlockLit : BlockDim;
            if (dash != null)
                dash.color = body.StrideCd > 0.05 ? DashReady : (dashHeld ? DashHeld : DashReady);
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
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
