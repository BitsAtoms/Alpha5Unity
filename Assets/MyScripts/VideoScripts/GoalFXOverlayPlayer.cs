using System.Collections;
using UnityEngine;

public class GoalFXOverlayPlayer : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup canvasGroup;
    public Animator animator;

    [Header("Animator state names")]
    public string golState = "Gol";
    public string falloState = "Fallo";

    [Header("Durations (seconds)")]
    public float golDuration = 5f;
    public float falloDuration = 5f;

    [Header("SFX (Gol/Fallo)")]
    public AudioSource sfxSource;
    public AudioClip golClip;
    public AudioClip falloClip;

    [Header("Music ducking")]
    public AudioSource musicSource;
    [Range(0f, 1f)] public float musicNormalVolume = 0.4f;   // tu volumen normal
    [Range(0f, 1f)] public float musicDuckedVolume = 0.12f;  // volumen durante Gol/Fallo
    public float duckFadeTime = 0.15f;                       // tiempo de bajada/subida

    Coroutine routine;
    Coroutine duckRoutine;

    void Awake()
    {
        HideInstant();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        // Si asignaste musicSource en Inspector, guarda su volumen como normal automáticamente
        if (musicSource)
            musicNormalVolume = musicSource.volume;
    }

    public void PlayGol()   => Play(golState, golDuration, golClip);
    public void PlayFallo() => Play(falloState, falloDuration, falloClip);

    void Play(string stateName, float duration, AudioClip clip)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoPlay(stateName, duration, clip));
    }

    IEnumerator CoPlay(string stateName, float duration, AudioClip clip)
    {
        // Baja música suave
        DuckMusic(true);

        ShowInstant();
        animator.Play(stateName, 0, 0f);

        // SFX
        if (sfxSource  && clip)
        {
            sfxSource.Stop();
            sfxSource.PlayOneShot(clip);
        }

        yield return new WaitForSeconds(duration);

        HideInstant();

        // Sube música suave
        DuckMusic(false);

        routine = null;
    }

    void DuckMusic(bool duck)
    {
        if (musicSource == null) return;

        float target = duck ? musicDuckedVolume : musicNormalVolume;

        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(FadeMusicTo(target, duckFadeTime));
    }

    IEnumerator FadeMusicTo(float targetVolume, float time)
    {
        float start = musicSource.volume;
        if (time <= 0f)
        {
            musicSource.volume = targetVolume;
            yield break;
        }

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            musicSource.volume = Mathf.Lerp(start, targetVolume, k);
            yield return null;
        }

        musicSource.volume = targetVolume;
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
