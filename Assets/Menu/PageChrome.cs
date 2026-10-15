using System.Runtime.InteropServices;
using UnityEngine;

namespace BattleTv.Menu
{
    public sealed class PageChrome : MonoBehaviour
    {
        [SerializeField] string screen = "intro";

        public string Screen => screen;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BattleTvChrome(string mode);
#endif

        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BattleTvChrome(screen);
#endif
        }
    }
}
