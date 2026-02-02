using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Nombres de escenas")]
    public string sceneAdulto = "ProyectoFinalAdulto";
    public string sceneNino = "ProyectoFinalNiño";

    public void PlayNormal()
    {
        EnsureSettings();
        GameSettings.I.SetMode(GameMode.Normal);
        SceneManager.LoadScene(sceneAdulto);
    }

    public void PlayNino()
    {
        EnsureSettings();
        GameSettings.I.SetMode(GameMode.Nino);
        SceneManager.LoadScene(sceneNino);
    }

    void EnsureSettings()
    {
        if (GameSettings.I != null) return;

        var go = new GameObject("GameSettings");
        go.AddComponent<GameSettings>();
    }
}
