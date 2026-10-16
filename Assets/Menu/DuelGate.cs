using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleTv.Menu
{
    public sealed class DuelGate : MonoBehaviour
    {
        public void OpenCard()
        {
            if (!ShowPick.BeginAutonomous(new System.Random()))
                return;
            SceneManager.LoadScene("Duel");
        }
    }
}
