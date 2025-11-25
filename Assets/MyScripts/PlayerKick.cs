using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerKick : MonoBehaviour
{
    [Header("Fuerza del chut")]
    public float baseKickForce = 7.5f;
    public float speedToForce = 1.5f;     // cuánto aporta la velocidad del jugador
    public float upwardBonus = 0.0f;      // levanta un poco el balón si quieres (0 = rasito)

    [Header("Disparo")]
    public bool requireKeyToKick = false;  // si false, chuta automáticamente al contacto
    public KeyCode kickKey = KeyCode.Space;
    public float reKickDelay = 0.25f;     // anti-spam de chuts en contacto continuo
    float lastKickTime = -99f;

    Rigidbody playerRb;

    void Awake()
    {
        playerRb = GetComponent<Rigidbody>();
    }

    void OnCollisionStay(Collision col)
    {
        if (!col.collider.CompareTag("Ball")) return;

        bool wantsKick = requireKeyToKick ? Input.GetKeyDown(kickKey) : true;
        if (!wantsKick) return;
        if (Time.time - lastKickTime < reKickDelay) return;

        Rigidbody ballRb = col.rigidbody;
        if (!ballRb) return;

        // Dirección del impulso: media de las normales de los puntos de contacto
        Vector3 avgNormal = Vector3.zero;
        foreach (var c in col.contacts)
            avgNormal += c.normal; // normal del Player apuntando hacia "fuera" -> hacia la pelota
        avgNormal.Normalize();

        // Si no hay normales por algún motivo, usa vector desde player a balón
        if (avgNormal == Vector3.zero)
            avgNormal = (ballRb.worldCenterOfMass - playerRb.worldCenterOfMass).normalized;

        // Añade un pequeño componente vertical si lo deseas
        Vector3 kickDir = avgNormal;
        kickDir.y += upwardBonus;
        kickDir.Normalize();

        // Magnitud de la fuerza: base + velocidad del jugador
        float playerSpeed = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z).magnitude;
        float forceMag = baseKickForce + playerSpeed * speedToForce;

        // Aplica impulso
        ballRb.AddForce(kickDir * forceMag, ForceMode.Impulse);

        // Arranca la ventana de 2 s para validar gol/fallo
        GameManager.I?.ArmShotWindow();

        lastKickTime = Time.time;
    }
}
