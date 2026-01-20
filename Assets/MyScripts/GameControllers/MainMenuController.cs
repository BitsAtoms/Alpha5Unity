using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Escena del juego (Build Index o nombre)")]
    public int gameSceneBuildIndex = 1;

    public void PlayNormal()
    {
        EnsureSettings();
        GameSettings.I.SetMode(GameMode.Normal);
        SceneManager.LoadScene(gameSceneBuildIndex);
    }

    public void PlayNino()
    {
        EnsureSettings();
        GameSettings.I.SetMode(GameMode.Nino);
        SceneManager.LoadScene(gameSceneBuildIndex);
    }

    void EnsureSettings()
    {
        if (GameSettings.I != null) return;

        var go = new GameObject("GameSettings");
        go.AddComponent<GameSettings>();
    }
}
