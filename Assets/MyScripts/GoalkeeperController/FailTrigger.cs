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
        Debug.Log($"[FailTrigger] Algo entró: {other.name}, tag={other.tag}");

        if (other.CompareTag(ballTag) == false)
        {
            Debug.Log("[FailTrigger] IGNORADO → No es la pelota");
            return;
        }

        if (GameManager.I == null)
        {
            Debug.Log("[FailTrigger] ERROR → GameManager.I es NULL");
            return;
        }

        if (!GameManager.I.CanShoot())
        {
            Debug.Log("[FailTrigger] IGNORADO → CanShoot() = false");
            return;
        }

        Debug.Log($"[FailTrigger] FALLASTE → obj={other.name}");
        GameManager.I.ShotFail();
    }

    void OnTriggerStay(Collider other)
    {
        if (!logOnStayForDebug) return;
        if (!other.CompareTag(ballTag)) return;

        Debug.Log($"[FailTrigger] OnTriggerStay con {other.name}");
    }
}
