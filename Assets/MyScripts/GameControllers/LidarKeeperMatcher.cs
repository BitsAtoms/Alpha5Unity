using System;
using UnityEngine;

public class LidarKeeperMatcher : MonoBehaviour
{
    [Header("Referencias")]
    public KeeperTracker keeperTracker;
    public SickLidarDetector lidarDetector; 
    public Transform goalBottomLeft; 
    public Transform goalTopRight;

    [Header("Sincronización")]
    public double maxTimeDiffSeconds = 0.12;

    [Header("Físicas de Unity")]
    [Tooltip("El radio de la pelota virtual en metros (ej: 0.11 para un balón de fútbol)")]
    public float ballRadius = 0.11f;
    [Tooltip("El Tag que tienen los huesos del portero")]
    public string keeperTag = "Goalkeeper";

    // --- VARIABLES PARA EL DEBUG VISUAL ---
    private Vector3 lastImpactPos;
    private bool hasImpact = false;

    void OnEnable() 
    {
        if (lidarDetector) lidarDetector.OnGoalDetected += OnGoalSample;
    }

    void OnDisable() 
    {
        if (lidarDetector) lidarDetector.OnGoalDetected -= OnGoalSample;
    }

    void OnGoalSample(float lat01, float h01, double tsSeconds) 
    {
        // 1. Comprobaciones de seguridad del estado del juego
        if (GameManager.I == null || !GameManager.I.CanShoot()) return;

        float travelTime = Time.time - GameManager.I.exactShotStartTime;
        
        // Opcional: Si sabes la distancia física de tu portería, puedes sacar la velocidad. 
        // Asumiendo 3 metros:
        float distanceMeters = 3.0f; 
        float speedMs = distanceMeters / travelTime;
        float speedKmh = speedMs * 3.6f;

        Debug.Log($"⏱️ [MÉTRICAS TIRO] Tiempo de vuelo: {travelTime:F3} seg | Vel. Aprox: {speedKmh:F1} km/h");

        // Validamos la sincronización de tiempo con el tracker del portero
        if (keeperTracker == null || !keeperTracker.TryGetNearest(tsSeconds, out var k)) {
            GameManager.I.GoalScored();
            return;
        }

        double dt = Math.Abs(k.ts - tsSeconds);
        if (dt > maxTimeDiffSeconds) return; 

        if (goalBottomLeft == null || goalTopRight == null) {
            Debug.LogError("[MATCH] Faltan las referencias de la portería (BottomLeft/TopRight)");
            return;
        }

        // 2. Calcular el punto exacto 3D en el mundo de Unity
        float worldX = Mathf.Lerp(goalBottomLeft.position.x, goalTopRight.position.x, lat01);
        float worldY = Mathf.Lerp(goalBottomLeft.position.y, goalTopRight.position.y, h01);
        float worldZ = Mathf.Lerp(goalBottomLeft.position.z, goalTopRight.position.z, lat01); 
        
        Vector3 impactPosition = new Vector3(worldX, worldY, worldZ);

        // Guardamos la posición para que OnDrawGizmos pueda dibujar la esfera roja
        lastImpactPos = impactPosition;
        hasImpact = true;

        // --- LAS 2 LÍNEAS MÁGICAS ---
        // 1. Obliga a Unity a mover los colliders físicos a donde está la animación AHORA MISMO
        Physics.SyncTransforms(); 

        // 2. Comprobación antibalas: Forzamos que busque en TODAS las capas (Physics.AllLayers)
        // y que detecte también colliders tipo Trigger por si acaso
        Collider[] hitColliders = Physics.OverlapSphere(impactPosition, ballRadius, Physics.AllLayers, QueryTriggerInteraction.Collide);
        // -----------------------------

        bool touchedKeeper = false;
        Debug.Log($"[DEBUG FÍSICAS] La esfera se creó y ha tocado {hitColliders.Length} objetos.");

        foreach (var hitCollider in hitColliders)
        {
            // Que nos chive el nombre y el Tag de TODO lo que toque
            Debug.Log($"[DEBUG FÍSICAS] -> Tocó: {hitCollider.gameObject.name} | Tag: {hitCollider.tag}");

            if (hitCollider.CompareTag(keeperTag))
            {
                touchedKeeper = true;
            }
        }
        // 4. Resolver la jugada
        if (touchedKeeper) {
            Debug.Log("🛡️ [MATCH] ¡PARADA! El balón físico chocó con el modelo del portero.");
            GameManager.I.ShotFail();
        } else {
            Debug.Log("⚽ [MATCH] ¡GOL! El balón pasó limpio.");
            GameManager.I.GoalScored();
        }
    }

    // --- MAGIA VISUAL PARA EL EDITOR ---
    void OnDrawGizmos()
    {
        // Dibuja la línea base de la portería en cyan
        if (goalBottomLeft != null && goalTopRight != null) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(goalBottomLeft.position, goalTopRight.position);
        }

        // Dibuja la esfera exacta donde el LiDAR detectó la pelota
        if (hasImpact) {
            // Usamos un rojo semitransparente para ver a través de la bola
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); 
            Gizmos.DrawSphere(lastImpactPos, ballRadius);
            
            // Y un borde rojo fuerte para que se vea claro el contorno
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastImpactPos, ballRadius);
        }
    }
}