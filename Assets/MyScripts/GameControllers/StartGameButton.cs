using UnityEngine;

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

        // 1) Activar HUD
        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
            Debug.Log("✅ [UI] hudRoot activado");
        }
        else Debug.LogWarning("⚠️ [UI] hudRoot NO asignado");

        // 2) Activar lector del TXT y forzar lectura inmediata
        if (keeperFlagReaderObject != null)
        {
            keeperFlagReaderObject.SetActive(true);
            Debug.Log("✅ [UI] keeperFlagReaderObject activado");

            var reader = keeperFlagReaderObject.GetComponent<KeeperMoveFlagReader>();
            if (reader != null)
            {
                reader.ForceReadNow();
                Debug.Log("✅ [UI] ForceReadNow() ejecutado");
            }
            else Debug.LogWarning("⚠️ [UI] KeeperMoveFlagReader NO está en keeperFlagReaderObject");
        }
        else Debug.LogWarning("⚠️ [UI] keeperFlagReaderObject NO asignado");

        // 3) Ocultar botón Start
        if (startButtonObject != null)
        {
            startButtonObject.SetActive(false);
            Debug.Log("✅ [UI] startButtonObject ocultado");
        }
        else Debug.LogWarning("⚠️ [UI] startButtonObject NO asignado");

        // 4) Empezar partida
        if (GameManager.I != null)
        {
            GameManager.I.BeginGame();
            Debug.Log("✅ [UI] GameManager.BeginGame() llamado");
        }
        else Debug.LogWarning("⚠️ [UI] GameManager.I es NULL (no hay GameManager activo)");
    }
}
