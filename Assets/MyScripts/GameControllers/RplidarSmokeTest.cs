using UnityEngine;

public class RplidarSmokeTest : MonoBehaviour
{
    public string comPort = "COM3";
    public int baudrate = 460800;
    public float logEverySeconds = 1.0f;

    private RplidarNative.ScanPoint[] _buf = new RplidarNative.ScanPoint[8192];
    private float _t;

    void OnEnable()
    {
        int c = RplidarNative.rl_connect(comPort, baudrate);
        Debug.Log($"[RPLIDAR TEST] connect={c}");

        int s = RplidarNative.rl_start_scan();
        Debug.Log($"[RPLIDAR TEST] start_scan={s}");
    }

    void OnDisable()
    {
        try { Debug.Log($"[RPLIDAR TEST] stop={RplidarNative.rl_stop()}"); } catch {}
        try { RplidarNative.rl_disconnect(); Debug.Log("[RPLIDAR TEST] disconnected"); } catch {}
    }

    void Update()
    {
        int n = RplidarNative.rl_grab_points(_buf, _buf.Length);
        if (n <= 0) return;

        _t += Time.unscaledDeltaTime;
        if (_t < logEverySeconds) return;
        _t = 0f;

        // Saca un punto “representativo”
        var p = _buf[0];
        Debug.Log($"[RPLIDAR TEST] points={n} example: angle={p.angleDeg:F1} dist={p.distMm:F0}mm q={p.quality}");
    }
}
