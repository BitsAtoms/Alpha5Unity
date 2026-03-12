using UnityEngine;

public class LidarImpactVisualizer : MonoBehaviour
{
    [Header("Referencias")]
    public LidarEventDetector lidarDetector;
    public Transform goalBottomLeft; // Usa los mismos que en KeeperTracker
    public Transform goalTopRight;
    
    [Header("Feedback Visual")]
    [Tooltip("Prefab de una esfera semitransparente o partícula")]
    public GameObject impactMarkerPrefab; 
    public float markerDuration = 2.0f;

    void OnEnable()
    {
        if (lidarDetector) lidarDetector.OnGoalDetected += ShowImpact;
    }

    void OnDisable()
    {
        if (lidarDetector) lidarDetector.OnGoalDetected -= ShowImpact;
    }

    private void ShowImpact(float lat01, float h01, double tsSeconds)
    {
        // 1. Console Log detallado de coordenadas normalizadas (0 a 1)
        Debug.Log($"🎯 [LIDAR IMPACTO] Latitud: {lat01:F3} | Altura: {h01:F3} | Timestamp: {tsSeconds}");

        if (goalBottomLeft == null || goalTopRight == null || impactMarkerPrefab == null) 
            return;

        // 2. Traducir coordenadas normalizadas al mundo 3D de Unity
        // Asumimos que lat01 mueve en el plano horizontal (X, Z) y h01 en el vertical (Y)
        float worldX = Mathf.Lerp(goalBottomLeft.position.x, goalTopRight.position.x, lat01);
        float worldZ = Mathf.Lerp(goalBottomLeft.position.z, goalTopRight.position.z, lat01);
        float worldY = Mathf.Lerp(goalBottomLeft.position.y, goalTopRight.position.y, h01);

        Vector3 impactPos = new Vector3(worldX, worldY, worldZ);

        // 3. Crear el marcador visual y destruirlo tras unos segundos
        GameObject marker = Instantiate(impactMarkerPrefab, impactPos, Quaternion.identity);
        Destroy(marker, markerDuration);
    }
}