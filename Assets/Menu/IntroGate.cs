using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleTv.Menu
{
    public sealed class IntroGate : MonoBehaviour
    {
        public void EnterMenu()
        {
            SceneManager.LoadScene("GameMenu");
        }
    }
}
