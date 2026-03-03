using UnityEngine;
using UnityEngine.EventSystems;

public class NeonButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}