using UnityEngine;

[DefaultExecutionOrder(-200)] // para que ejecute antes que casi todo
public class GoalkeeperStartupFix : MonoBehaviour
{
    public Animator animator;
    [Tooltip("Nombre EXACTO del estado Idle del Animator")]
    public string idleStateName = "Idle";

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        ForceIdleNow("[FIX] Awake");
    }

    void OnEnable()
    {
        ForceIdleNow("[FIX] OnEnable");
    }

    public void ForceIdleNow(string tag = "[FIX]")
    {
        if (!animator) return;

        // Esto evita que se quede con poses raras al reactivar
        animator.Rebind();
        animator.Update(0f);

        // Forzar el estado Idle
        animator.Play(idleStateName, 0, 0f);
        animator.Update(0f);

        Debug.Log($"{tag} Idle forzado + Rebind()");
    }
}
