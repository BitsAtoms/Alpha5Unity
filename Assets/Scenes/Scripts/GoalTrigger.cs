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
        if (GameManager.I == null || !GameManager.I.CanShoot()) return;
        if (!other.CompareTag(ballTag)) return;

        Debug.Log($"[GoalTrigger] GOOOL (obj: {other.name})");
        GameManager.I.GoalScored();
    }

    void OnTriggerStay(Collider other)
    {
        if (!logOnStayForDebug) return;
        if (!other.CompareTag(ballTag)) return;
        Debug.Log($"[GoalTrigger] OnTriggerStay con {other.name}");
    }
}
