using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FailTrigger : MonoBehaviour
{
    public string ballTag = "Ball";
    public bool logOnStayForDebug = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.I == null || !GameManager.I.CanShoot()) return;
        if (!other.CompareTag(ballTag)) return;

        Debug.Log($"[FailTrigger] FALLO pared de fondo (obj: {other.name})");
        GameManager.I.ShotFail();
    }

    void OnTriggerStay(Collider other)
    {
        if (!logOnStayForDebug) return;
        if (!other.CompareTag(ballTag)) return;
        Debug.Log($"[FailTrigger] OnTriggerStay con {other.name}");
    }
}
