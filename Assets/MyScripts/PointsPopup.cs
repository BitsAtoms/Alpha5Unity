using UnityEngine;
using TMPro;
using System.Collections;

public class PointsPopup : MonoBehaviour
{
    public TMP_Text text;
    public float duration = 1.2f;

    Coroutine currentRoutine;

    void Awake()
    {
        if (!text)
            text = GetComponent<TMP_Text>();

        gameObject.SetActive(false);
    }

    public void ShowPoints(int points)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(Co_Show(points));
    }

    IEnumerator Co_Show(int points)
    {
        gameObject.SetActive(true);
        text.text = $"+{points}";
        yield return new WaitForSeconds(duration);
        gameObject.SetActive(false);
    }
}
