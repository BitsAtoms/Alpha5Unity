using UnityEngine;
using TMPro;
using System.Collections;

public class PointsPopup : MonoBehaviour
{
    [Header("Referencias")]
    public TMP_Text text;
    public RectTransform rect;

    [Header("Animación")]
    public float duration = 1.2f;
    public float floatPixels = 80f;

    Vector2 startAnchoredPos;

    void Awake()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        if (!text) text = GetComponentInChildren<TMP_Text>();

        // Guardamos posición inicial UI
        startAnchoredPos = rect.anchoredPosition;

        // Oculto al inicio
        gameObject.SetActive(false);
    }

    public void ShowPoints(int points)
    {
        if (text == null || rect == null)
        {
            Debug.LogWarning("[PointsPopup] Falta TMP_Text o RectTransform");
            return;
        }

        StopAllCoroutines();

        // Texto + puntos
        text.text = $"+{points}";

        // Aseguramos que sea visible
        text.alpha = 1f;

        // Forzamos que se vea SIEMPRE (centro de pantalla)
        rect.anchoredPosition = Vector2.zero;

        gameObject.SetActive(true);
        StartCoroutine(Co_Show());
    }

    IEnumerator Co_Show()
    {
        float t = 0f;
        Vector2 from = rect.anchoredPosition;
        Vector2 to = from + new Vector2(0f, floatPixels);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rect.anchoredPosition = Vector2.Lerp(from, to, k);
            yield return null;
        }

        // volver a su sitio original por si luego quieres usarlo fijo
        rect.anchoredPosition = startAnchoredPos;

        gameObject.SetActive(false);
    }
}
