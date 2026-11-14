using UnityEngine;
using UnityEngine.SceneManagement;

namespace UzbCarParking.Core
{
    /// <summary>Sahnalarni yuklash. Har doim pauza holatini tiklaydi.</summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(sceneName);
        }

        public static void LoadMainMenu()
        {
            Load(GameConstants.MainMenuScene);
        }

        public static void LoadAvtodrom()
        {
            Load(GameConstants.AvtodromScene);
        }

        public static void ReloadCurrent()
        {
            Load(SceneManager.GetActiveScene().name);
        }
    }
}
