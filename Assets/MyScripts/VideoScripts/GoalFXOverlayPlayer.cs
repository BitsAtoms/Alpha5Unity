using System.Collections;
using UnityEngine;

public class GoalFXOverlayPlayer : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup; // del panel GoalFXOverlay
    public Animator animator;       // Animator del GoalFXImage

    [Header("Animator state names")]
    public string golState = "Gol";
    public string falloState = "Fallo";

    [Header("Durations (seconds)")]
    public float golDuration = 5f;
    public float falloDuration = 5f;

    Coroutine routine;

    void Awake()
    {
        HideInstant();
    }

    public void PlayGol()   => Play(golState, golDuration);
    public void PlayFallo() => Play(falloState, falloDuration);

    void Play(string stateName, float duration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoPlay(stateName, duration));
    }

    IEnumerator CoPlay(string stateName, float duration)
    {
        ShowInstant();

        // reproducir desde el principio
        animator.Play(stateName, 0, 0f);

        yield return new WaitForSeconds(duration);

        HideInstant();
        routine = null;
    }

    void ShowInstant()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void HideInstant()
    {
        if (animator) animator.Rebind();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
