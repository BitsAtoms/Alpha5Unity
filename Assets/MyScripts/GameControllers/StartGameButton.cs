using UnityEngine;
using System.Collections;

public class StartGameButton : MonoBehaviour
{
    public GameObject hudRoot;
    public GameObject keeperFlagReaderObject;

    void Update()
    {
        // Pulsar SPACE para empezar la ronda
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(Co_StartGame());
        }
    }

    IEnumerator Co_StartGame()
    {
        if (hudRoot) hudRoot.SetActive(true);
        if (keeperFlagReaderObject) keeperFlagReaderObject.SetActive(true);

        yield return null;

        if (GameManager.I)
            GameManager.I.BeginGame();
    }

    public void ShowForNewRound()
    {
        if (hudRoot) hudRoot.SetActive(false);
    }
}