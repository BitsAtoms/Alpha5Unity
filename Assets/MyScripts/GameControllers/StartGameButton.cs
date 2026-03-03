using UnityEngine;
using System.Collections;

public class StartGameButton : MonoBehaviour
{
    public GameObject hudRoot;
    public GameObject startButtonObject;
    public GameObject keeperFlagReaderObject;

    public void StartGame() { StartCoroutine(Co_StartGame()); }

    IEnumerator Co_StartGame() {
        if (hudRoot) hudRoot.SetActive(true);
        if (keeperFlagReaderObject) keeperFlagReaderObject.SetActive(true);
        if (startButtonObject) startButtonObject.SetActive(false);
        yield return null;
        if (GameManager.I) GameManager.I.BeginGame();
    }

    public void ShowForNewRound() {
        if (startButtonObject) startButtonObject.SetActive(true);
        if (hudRoot) hudRoot.SetActive(false);
    }
}