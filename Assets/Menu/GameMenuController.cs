using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleTv.Menu
{
    public sealed class GameMenuController : MonoBehaviour
    {
        static readonly Color Gold = new Color(0xf5 / 255f, 0x9e / 255f, 0x0b / 255f, 1f);
        static readonly Color Ink = new Color(0x1a / 255f, 0x12 / 255f, 0x06 / 255f, 1f);

        [SerializeField] Button[] modes;
        [SerializeField] int[] modeValues;

        void Start()
        {
            Paint();
        }

        public void Pick(int mode)
        {
            ShowPick.BeginDraft(mode);
            Paint();
            SceneManager.LoadScene("TeamSelect");
        }

        void Paint()
        {
            if (modes == null || modeValues == null)
                return;
            var count = Mathf.Min(modes.Length, modeValues.Length);
            for (var i = 0; i < count; i++)
            {
                var on = modeValues[i] == ShowPick.Mode;
                var face = modes[i].targetGraphic as Image;
                if (face != null)
                    face.color = Color.white;
                var colors = modes[i].colors;
                colors.normalColor = on ? Gold : Ink;
                colors.highlightedColor = on ? new Color(1f, 0.72f, 0.2f, 1f) : new Color(0.2f, 0.14f, 0.06f, 1f);
                colors.pressedColor = Gold;
                colors.selectedColor = colors.normalColor;
                colors.colorMultiplier = 1f;
                modes[i].colors = colors;
                var label = modes[i].GetComponentInChildren<Text>(true);
                if (label != null)
                    label.color = on ? Ink : Gold;
            }
        }
    }
}
