using UnityEngine;
using System.Collections;

public class StartGameButton : MonoBehaviour
{
    [Header("UI")]
    public GameObject hudRoot;
    public GameObject startButtonObject;

    [Header("Flag Reader (TXT)")]
    public GameObject keeperFlagReaderObject;

    public void StartGame()
    {
        Debug.Log("✅ [UI] StartGame() llamado");
        StartCoroutine(Co_StartGame());
    }

    IEnumerator Co_StartGame()
{
    if (hudRoot) hudRoot.SetActive(true);

    if (keeperFlagReaderObject) keeperFlagReaderObject.SetActive(true);

    if (startButtonObject) startButtonObject.SetActive(false);

    yield return null;

    if (GameManager.I != null) GameManager.I.BeginGame();
}

    // ✅ Llamar desde GameManager al iniciar cada ronda
    public void ShowForNewRound()
    {
        if (startButtonObject != null)
            startButtonObject.SetActive(true);

        if (hudRoot != null)
            hudRoot.SetActive(false);

        // OJO: NO desactivamos keeperFlagReaderObject aquí; déjalo activo si quieres que siga leyendo.
        Debug.Log("✅ [UI] Botón Start visible para nueva ronda");
    }
}
