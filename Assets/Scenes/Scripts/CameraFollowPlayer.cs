using UnityEngine;
using System.Collections;

public class CameraBehindFixedAuto : MonoBehaviour
{
    [Header("Jugador")]
    public Transform player;

    [Header("(Opcional) Referencia de campo")]
    [Tooltip("Asigna el centro de la portería o un objeto que apunte hacia 'delante del campo'. " +
             "Si lo asignas, la cámara se orienta instantáneamente; si no, aprende al detectar movimiento del player.")]
    public Transform goalOrFieldReference;

    [Header("Colocación de cámara")]
    public float distance = 12f;   // qué tan atrás del jugador
    public float height   = 5f;    // altura de la cámara
    public float lookHeight = 1.5f; // altura del punto que mira (torso)

    [Header("Auto–detección por movimiento (si no hay referencia)")]
    public float learnTimeout = 2f;     // tiempo máximo para aprender (s)
    public float minMoveSpeed = 0.25f;  // velocidad mínima para considerar que se mueve

    Vector3 fixedPosition;
    bool cameraFixed = false;
    bool learning = false;

    void Start()
    {
        if (!player)
        {
            Debug.LogWarning("[CameraBehindFixedAuto] Falta asignar 'player'. Desactivo.");
            enabled = false;
            return;
        }

        if (TryInitFromReference())
            return;

        // Si no hay referencia, aprendemos mirando el movimiento del jugador.
        StartCoroutine(Co_LearnDirectionFromMovement());
    }

    bool TryInitFromReference()
    {
        if (!goalOrFieldReference) return false;

        // Dirección "hacia la portería" en el plano XZ
        Vector3 toGoal = goalOrFieldReference.position - player.position;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude < 0.001f) return false;

        Vector3 fieldForward = toGoal.normalized; // hacia portería
        Vector3 behindDir = -fieldForward;        // detrás del player respecto al campo

        fixedPosition = player.position + behindDir * distance;
        fixedPosition.y += height;

        transform.position = fixedPosition;
        transform.LookAt(player.position + Vector3.up * lookHeight);

        cameraFixed = true;
        Debug.Log("[CameraBehindFixedAuto] Orientación desde referencia establecida.");
        return true;
    }

    IEnumerator Co_LearnDirectionFromMovement()
    {
        learning = true;

        float t = 0f;
        Vector3 lastPos = player.position;

        // Espera a detectar una dirección de movimiento suficientemente clara en XZ
        while (t < learnTimeout && !cameraFixed)
        {
            yield return null;
            t += Time.deltaTime;

            Vector3 delta = player.position - lastPos;
            lastPos = player.position;

            // Considera solo movimiento horizontal (XZ)
            delta.y = 0f;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

            if (speed >= minMoveSpeed)
            {
                Vector3 moveDir = delta.normalized; // hacia donde se mueve
                Vector3 behindDir = -moveDir;       // detrás del jugador
                fixedPosition = player.position + behindDir * distance;
                fixedPosition.y += height;

                transform.position = fixedPosition;
                transform.LookAt(player.position + Vector3.up * lookHeight);

                cameraFixed = true;
                Debug.Log("[CameraBehindFixedAuto] Orientación aprendida por movimiento.");
            }
        }

        // Si no se movió a tiempo, usa un fallback suave: deja la cámara donde ya esté pero mirando al jugador
        if (!cameraFixed)
        {
            fixedPosition = transform.position; // no mover
            cameraFixed = true;
            Debug.Log("[CameraBehindFixedAuto] No hubo movimiento para aprender. Mantengo posición actual.");
        }

        learning = false;
    }

    void LateUpdate()
    {
        if (!cameraFixed) return; // aún aprendiendo u orientándose

        // Mantén SIEMPRE la posición fija
        transform.position = fixedPosition;

        // Solo gira para mirar al jugador según se mueva
        transform.LookAt(player.position + Vector3.up * lookHeight);
    }

    // Llamar si cambias el lado del campo a mitad de partida y quieres recolocar la cámara detrás de nuevo
    public void ReorientBehindPlayerNow(Vector3 newFieldForward)
    {
        newFieldForward.y = 0f;
        if (newFieldForward == Vector3.zero) return;

        Vector3 behindDir = -newFieldForward.normalized;
        fixedPosition = player.position + behindDir * distance;
        fixedPosition.y += height;

        transform.position = fixedPosition;
        transform.LookAt(player.position + Vector3.up * lookHeight);
        cameraFixed = true;
        learning = false;

        Debug.Log("[CameraBehindFixedAuto] Reorientación manual realizada.");
    }
}
