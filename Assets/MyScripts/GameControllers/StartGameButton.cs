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
        // 1) HUD ON
        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
            Debug.Log("✅ [UI] hudRoot activado");
        }

        // 2) Reader ON + prime (sin pulso inicial)
        if (keeperFlagReaderObject != null)
        {
            keeperFlagReaderObject.SetActive(true);
            Debug.Log("✅ [UI] keeperFlagReaderObject activado");

            yield return null; // 1 frame para que Awake/OnEnable corran

            var reader = keeperFlagReaderObject.GetComponent<KeeperMoveFlagReaderTimestamp>();
            if (reader != null)
            {
                reader.PrimeFromFile();
                reader.ClearPulse();
                Debug.Log("✅ [UI] Reader primado (sin pulso) y pulso limpiado");
            }
            else Debug.LogWarning("⚠️ [UI] KeeperMoveFlagReaderTimestamp NO está en keeperFlagReaderObject");
        }
        else Debug.LogWarning("⚠️ [UI] keeperFlagReaderObject NO asignado");

        // 3) Botón OFF
        if (startButtonObject != null)
        {
            startButtonObject.SetActive(false);
            Debug.Log("✅ [UI] startButtonObject ocultado");
        }

        yield return null;

        // 4) Avisar GameManager
        if (GameManager.I != null)
        {
            GameManager.I.BeginGame();
            Debug.Log("✅ [UI] GameManager.BeginGame() llamado");
        }
        else Debug.LogWarning("⚠️ [UI] GameManager.I es NULL");
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
