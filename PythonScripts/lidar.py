import asyncio
import time
import math
import os
from collections import defaultdict
from statistics import median

from rplidarc1.scanner import RPLidar

# =========================
# UTILIDADES (PORTABLE)
# =========================
def find_project_root(start_path, project_name="Alpha5Unity"):
    path = start_path
    while True:
        if os.path.basename(path) == project_name:
            return path
        new_path = os.path.dirname(path)
        if new_path == path:
            raise RuntimeError("No se encontró la carpeta Alpha5Unity")
        path = new_path

def ts_ms() -> int:
    return int(time.time() * 1000)

def atomic_overwrite(path: str, text: str):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        f.write(text)
    os.replace(tmp, path)

def clamp01(v: float) -> float:
    return max(0.0, min(1.0, v))

def normalize_coords(lat_m: float, h_m: float, lat_max_m: float, height_max_m: float):
    lat01 = clamp01(lat_m / lat_max_m)
    h01 = clamp01(h_m / height_max_m)
    return lat01, h01

def write_result_coords(path: str, lat01: float, h01: float):
    # salida: lat01,h01,timestamp_ms
    atomic_overwrite(path, f"{lat01:.4f},{h01:.4f},{ts_ms()}\n")


# =========================
# CONFIGURACIÓN
# =========================
PORT = "COM4"
BAUDRATE = 460800

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = find_project_root(BASE_DIR, "Alpha5Unity")
CONFIG_DIR = os.path.join(PROJECT_ROOT, "Config")
os.makedirs(CONFIG_DIR, exist_ok=True)

GOAL_RESULT_FILE = os.path.join(CONFIG_DIR, "goal_result.txt")

# Área de detección (2x2 metros)
LAT_MAX_M = 2.0
HEIGHT_MAX_M = 2.0

# Sector LIDAR (90° hacia arriba-derecha)
ANGLE_OFFSET_DEG = 270.0
ANGLE_MIN = 0.0
ANGLE_MAX = 90.0

# Filtros básicos
MIN_DIST_MM = 30.0
MAX_DIST_MM = 8000.0
MIN_QUALITY = 0

# PARÁMETROS DE DETECCIÓN
BASELINE_SECONDS = 2.0
ANGLE_BIN_DEG = 2
DELTA_MM = 150.0
MIN_POINTS_EVENT = 3
EVENT_COOLDOWN_S = 0.3
BASELINE_ALPHA = 0.97

DIAGNOSTIC_MODE = True


# =========================
# GEOMETRÍA / HELPERS
# =========================
def wrap_deg(a: float) -> float:
    return a % 360.0

def angle_corrected(a_deg: float) -> float:
    return wrap_deg(a_deg - ANGLE_OFFSET_DEG)

def angle_in_sector(a_deg: float, a_min: float, a_max: float) -> bool:
    a_deg = wrap_deg(a_deg)
    a_min = wrap_deg(a_min)
    a_max = wrap_deg(a_max)
    if a_min <= a_max:
        return a_min <= a_deg <= a_max
    return a_deg >= a_min or a_deg <= a_max

def bin_angle(a_deg: float) -> int:
    return int(wrap_deg(a_deg) // ANGLE_BIN_DEG)

def polar_to_wall_coords(angle_corr_deg: float, dist_mm: float):
    dist_m = dist_mm / 1000.0
    angle_rad = math.radians(angle_corr_deg)
    lateral_m = dist_m * math.sin(angle_rad)
    altura_m = dist_m * math.cos(angle_rad)
    return lateral_m, altura_m


# =========================
# DIAGNÓSTICO
# =========================
async def diagnostic_scan(lidar: RPLidar, duration: float = 3.0):
    print("\n" + "="*70)
    print("🔍 MODO DIAGNÓSTICO - Analizando datos del LIDAR")
    print("="*70)

    t_end = time.time() + duration
    sector_points = 0
    total = 0

    while time.time() < t_end:
        try:
            data = await asyncio.wait_for(lidar.output_queue.get(), timeout=0.5)
        except asyncio.TimeoutError:
            continue

        if not isinstance(data, dict):
            continue

        a = data.get("a_deg")
        d = data.get("d_mm")
        if a is None or d is None:
            continue

        total += 1
        a = float(a)
        d = float(d)

        a_corr = angle_corrected(a)
        if angle_in_sector(a_corr, ANGLE_MIN, ANGLE_MAX):
            sector_points += 1

    print(f"Total puntos: {total} | En sector: {sector_points}")
    print("="*70 + "\n")
    return sector_points > 0


# =========================
# LECTURA NORMAL
# =========================
async def read_frames_by_revolution(lidar: RPLidar):
    prev_angle = None
    frame = []

    while not lidar.stop_event.is_set():
        try:
            data = await asyncio.wait_for(lidar.output_queue.get(), timeout=0.2)
        except asyncio.TimeoutError:
            continue

        if not isinstance(data, dict):
            continue

        a = data.get("a_deg")
        d = data.get("d_mm")
        q = data.get("q", 0)

        if a is None or d is None:
            continue

        a = float(a)
        d = float(d)
        q = int(q)

        if d <= 0 or d < MIN_DIST_MM or d > MAX_DIST_MM:
            continue
        if q < MIN_QUALITY:
            continue

        a_corr = angle_corrected(a)

        if not angle_in_sector(a_corr, ANGLE_MIN, ANGLE_MAX):
            continue

        if prev_angle is not None and (a_corr + 20.0) < prev_angle:
            if frame:
                yield frame
            frame = []

        prev_angle = a_corr
        frame.append({"a_corr": a_corr, "d": d, "q": q})


async def build_baseline(lidar: RPLidar, seconds: float):
    per_bin = defaultdict(list)
    t_end = time.time() + seconds

    async for frame in read_frames_by_revolution(lidar):
        for p in frame:
            per_bin[bin_angle(p["a_corr"])].append(p["d"])
        if time.time() >= t_end:
            break

    baseline = {}
    for b, ds in per_bin.items():
        if ds:
            baseline[b] = median(ds)

    return baseline


# =========================
# MAIN
# =========================
async def main():
    print("[LIDAR] Config dir:", CONFIG_DIR)
    print("[LIDAR] goal_result:", GOAL_RESULT_FILE)

    lidar = RPLidar(PORT, BAUDRATE)
    try:
        print(f"Health check: {lidar.healthcheck()}")
        lidar.stop_event.clear()

        async with asyncio.TaskGroup() as tg:
            tg.create_task(lidar.simple_scan(make_return_dict=True))

            if DIAGNOSTIC_MODE:
                await asyncio.sleep(0.5)
                ok = await diagnostic_scan(lidar, duration=3.0)
                if not ok:
                    print("❌ No hay puntos en sector. Revisa OFFSET/orientación.")
                    return

            print(f"Construyendo baseline ({BASELINE_SECONDS}s)...")
            baseline = await build_baseline(lidar, BASELINE_SECONDS)
            if not baseline:
                print("❌ No se pudo crear baseline.")
                return

            last_event_ts = 0.0
            event_count = 0

            print("🎯 Detectando evento (coords lat01,h01,ts_ms)...")

            async for frame in read_frames_by_revolution(lidar):
                candidates = []

                for p in frame:
                    b = bin_angle(p["a_corr"])
                    base_d = baseline.get(b)
                    if base_d is None:
                        continue

                    diff = base_d - p["d"]
                    if diff >= DELTA_MM:
                        lat_m, h_m = polar_to_wall_coords(p["a_corr"], p["d"])

                        # Validación simple dentro del área 2x2
                        if lat_m < 0 or h_m < 0:
                            continue
                        if lat_m > LAT_MAX_M or h_m > HEIGHT_MAX_M:
                            continue

                        candidates.append({
                            "d": p["d"],
                            "lat_m": lat_m,
                            "h_m": h_m,
                            "diff": diff,
                            "a_corr": p["a_corr"]
                        })


                        # baseline adaptativo
                        baseline[b] = BASELINE_ALPHA * baseline[b] + (1 - BASELINE_ALPHA) * p["d"]

                now = time.time()
                if len(candidates) >= MIN_POINTS_EVENT and (now - last_event_ts) >= EVENT_COOLDOWN_S:
                    # Elegimos el candidato más cercano (menor distancia)
                    best = max(candidates, key=lambda c: (c["diff"], -c["d"]))
                    lat01, h01 = normalize_coords(best["lat_m"], best["h_m"], LAT_MAX_M, HEIGHT_MAX_M)

                    write_result_coords(GOAL_RESULT_FILE, lat01, h01)

                    last_event_ts = now
                    event_count += 1
                    print(f"⚽ EVENTO #{event_count} -> escrito: {lat01:.4f},{h01:.4f},{ts_ms()}")
                    print(f"[BEST] angle={best['a_corr']:.1f}° lat_m={best['lat_m']:.2f} h_m={best['h_m']:.2f} diff={best['diff']:.0f} d={best['d']:.0f}")

    except KeyboardInterrupt:
        print("Deteniendo...")
        lidar.stop_event.set()
    finally:
        lidar.reset()
        print("Reset completado.")


if __name__ == "__main__":
    asyncio.run(main())
