using System.Runtime.InteropServices;

public static class RplidarNative
{
    const string DLL = "rplidar_unity"; // rplidar_unity.dll en Assets/Plugins/x86_64/

    [StructLayout(LayoutKind.Sequential)]
    public struct ScanPoint
    {
        public float angleDeg; // 0..360
        public float distMm;   // mm
        public int quality;    // 0..?
    }

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rl_connect([MarshalAs(UnmanagedType.LPStr)] string comPort, int baudrate);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rl_start_scan();

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rl_grab_points([Out] ScanPoint[] points, int maxPoints);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern int rl_stop();

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    public static extern void rl_disconnect();
}
