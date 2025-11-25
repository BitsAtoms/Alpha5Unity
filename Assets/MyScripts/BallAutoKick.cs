using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallAutoKick : MonoBehaviour
{
    [Header("Detección")]
    public string playerTag = "Player";

    [Header("Fuerza del impulso")]
    public float baseKickForce = 6.5f;
    public float speedToForce = 1.2f;
    public float upwardBonus = 0.0f;

    [Header("Anti-repetición")]
    public float reKickDelay = 0.25f;
    float lastKickTime = -99f;

    Rigidbody ballRb;

    void Awake()
    {
        ballRb = GetComponent<Rigidbody>();
        ballRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        ballRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnCollisionEnter(Collision col)
    {
        if (!col.collider.CompareTag(playerTag)) return;

        if (GameManager.I == null)
        {
            Debug.LogWarning("[BallAutoKick] No hay GameManager.I");
            return;
        }

        if (!GameManager.I.CanShoot())
        {
            Debug.Log("[BallAutoKick] Ignorado: todavía no se puede chutar (entre rondas)");
            return;
        }

        // Armar tiro
        GameManager.I.ArmShotWindow();

        // Anti-spam
        if (Time.time - lastKickTime < reKickDelay) return;

        // Dirección del impulso: normal promedio
        Vector3 avgNormal = Vector3.zero;
        foreach (var c in col.contacts) avgNormal += c.normal;
        avgNormal.Normalize();
        if (avgNormal == Vector3.zero)
            avgNormal = (transform.position - col.collider.bounds.center).normalized;

        Vector3 kickDir = avgNormal;
        kickDir.y += upwardBonus;
        kickDir.Normalize();

        float playerSpeed = 0f;
        var playerRb = col.rigidbody;
        if (playerRb) playerSpeed = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z).magnitude;

        float forceMag = baseKickForce + playerSpeed * speedToForce;

        ballRb.AddForce(kickDir * forceMag, ForceMode.Impulse);
        lastKickTime = Time.time;

        Debug.Log("[BallAutoKick] Impulso aplicado y tiro ARMADO");
    }
}
