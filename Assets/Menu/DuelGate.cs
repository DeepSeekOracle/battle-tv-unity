using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleTv.Menu
{
    public sealed class DuelGate : MonoBehaviour
    {
        public void OpenCard()
        {
            if (ShowPick.Mode != 1)
                return;
            SceneManager.LoadScene("Duel");
        }
    }
}
