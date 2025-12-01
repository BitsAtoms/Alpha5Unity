using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
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
        Debug.Log($"[GoalTrigger] Algo entró: {other.name}, tag={other.tag}");

        if (other.CompareTag(ballTag) == false)
        {
            Debug.Log("[GoalTrigger] IGNORADO → No es la pelota");
            return;
        }

        if (GameManager.I == null)
        {
            Debug.Log("[GoalTrigger] ERROR → GameManager.I es NULL");
            return;
        }

        if (!GameManager.I.CanShoot())
        {
            Debug.Log("[GoalTrigger] IGNORADO → CanShoot() = false");
            return;
        }

        Debug.Log($"[GoalTrigger] GOOOOL → obj={other.name}");
        GameManager.I.GoalScored();
    }

    void OnTriggerStay(Collider other)
    {
        if (!logOnStayForDebug) return;
        if (!other.CompareTag(ballTag)) return;

        Debug.Log($"[GoalTrigger] OnTriggerStay con {other.name}");
    }
}
