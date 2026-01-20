import asyncio
import time
import math
import os
from collections import defaultdict
from statistics import median

from rplidarc1.scanner import RPLidar


# =========================
# CONFIGURACIÓN
# =========================
PORT = "COM3"
BAUDRATE = 460800

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_FILE = os.path.join(BASE_DIR, "Config", "detection_ball.txt")
os.makedirs(os.path.dirname(OUT_FILE), exist_ok=True)

# Área de detección (2x2 metros)
LAT_MAX_M = 2.0
HEIGHT_MAX_M = 2.0

# Sector LIDAR (90° hacia arriba-derecha)
ANGLE_OFFSET_DEG = 270.0
ANGLE_MIN = 0.0
ANGLE_MAX = 90.0

# Filtros básicos - RELAJADOS PARA DIAGNÓSTICO
MIN_DIST_MM = 30.0       # Muy bajo para captar todo
MAX_DIST_MM = 8000.0     # Muy alto para captar todo
MIN_QUALITY = 0          # Sin filtro de calidad

# PARÁMETROS DE DETECCIÓN
BASELINE_SECONDS = 2.0
ANGLE_BIN_DEG = 2
DELTA_MM = 150.0
MIN_POINTS_EVENT = 3
EVENT_COOLDOWN_S = 0.3
BASELINE_ALPHA = 0.97

# MODO DIAGNÓSTICO
DIAGNOSTIC_MODE = True


# =========================
# UTILIDADES
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

def clamp01(v: float) -> float:
    return max(0.0, min(1.0, v))

def polar_to_wall_coords(angle_corr_deg: float, dist_mm: float):
    dist_m = dist_mm / 1000.0
    angle_rad = math.radians(angle_corr_deg)
    lateral_m = dist_m * math.sin(angle_rad)
    altura_m = dist_m * math.cos(angle_rad)
    return lateral_m, altura_m

def normalize_coords(lat_m: float, h_m: float):
    lat01 = clamp01(lat_m / LAT_MAX_M)
    h01 = clamp01(h_m / HEIGHT_MAX_M)
    return lat01, h01

def atomic_overwrite(path: str, text: str):
    tmp = path + ".tmp"
    try:
        with open(tmp, "w", encoding="utf-8") as f:
            f.write(text)
        os.replace(tmp, path)
    except Exception:
        time.sleep(0.02)


# =========================
# DIAGNÓSTICO
# =========================
async def diagnostic_scan(lidar: RPLidar, duration: float = 5.0):
    """Escanea y muestra TODOS los datos recibidos"""
    print("\n" + "="*70)
    print("🔍 MODO DIAGNÓSTICO - Analizando datos del LIDAR")
    print("="*70)
    
    all_angles = []
    all_distances = []
    sector_points = []
    t_end = time.time() + duration
    point_count = 0
    
    while time.time() < t_end:
        try:
            data = await asyncio.wait_for(lidar.output_queue.get(), timeout=0.5)
        except asyncio.TimeoutError:
            print("⚠️  Timeout esperando datos...")
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
        point_count += 1
        
        all_angles.append(a)
        all_distances.append(d)
        
        # Calcular ángulo corregido
        a_corr = angle_corrected(a)
        
        # Verificar si está en sector
        in_sector = angle_in_sector(a_corr, ANGLE_MIN, ANGLE_MAX)
        
        if in_sector:
            sector_points.append({
                'raw': a,
                'corr': a_corr,
                'dist': d,
                'q': q
            })
        
        # Mostrar primeros 20 puntos
        if point_count <= 20:
            status = "✅ EN SECTOR" if in_sector else "❌ FUERA"
            print(f"  Punto {point_count}: raw={a:.1f}° corr={a_corr:.1f}° dist={d:.0f}mm q={q} | {status}")
    
    # Resumen
    print("\n" + "="*70)
    print("📊 RESUMEN DEL DIAGNÓSTICO")
    print("="*70)
    print(f"⏱️  Duración: {duration:.1f}s")
    print(f"📍 Puntos totales recibidos: {point_count}")
    print(f"✅ Puntos en sector objetivo (0-90°): {len(sector_points)}")
    
    if all_angles:
        print(f"\n📐 Ángulos RAW recibidos:")
        print(f"   Mínimo: {min(all_angles):.1f}°")
        print(f"   Máximo: {max(all_angles):.1f}°")
        print(f"   Rango completo: {min(all_angles):.1f}° a {max(all_angles):.1f}°")
    
    if sector_points:
        print(f"\n✅ Puntos en sector (corregidos 0-90°):")
        sector_angles = [p['corr'] for p in sector_points]
        sector_dists = [p['dist'] for p in sector_points]
        print(f"   Ángulo mín: {min(sector_angles):.1f}°")
        print(f"   Ángulo máx: {max(sector_angles):.1f}°")
        print(f"   Distancia mín: {min(sector_dists):.0f}mm")
        print(f"   Distancia máx: {max(sector_dists):.0f}mm")
        print(f"   Distancia med: {sum(sector_dists)/len(sector_dists):.0f}mm")
        
        print(f"\n📋 Primeros 10 puntos en sector:")
        for i, p in enumerate(sector_points[:10]):
            print(f"   {i+1}. raw={p['raw']:.1f}° → corr={p['corr']:.1f}° | {p['dist']:.0f}mm | q={p['q']}")
    else:
        print(f"\n❌ NO HAY PUNTOS EN EL SECTOR 0-90°")
        print(f"\n🔧 POSIBLES CAUSAS:")
        print(f"   1. ANGLE_OFFSET_DEG incorrecto (actual: {ANGLE_OFFSET_DEG}°)")
        print(f"   2. El LIDAR no está apuntando hacia el área de 2x2m")
        print(f"   3. No hay objetos en el rango del sector")
        
        if all_angles:
            # Sugerir offset correcto
            raw_min = min(all_angles)
            raw_max = max(all_angles)
            print(f"\n💡 SUGERENCIA DE CORRECCIÓN:")
            print(f"   Ángulos raw detectados: {raw_min:.1f}° a {raw_max:.1f}°")
            
            # Si queremos que estos ángulos mapeen a 0-90°, el offset debería ser:
            suggested_offset = raw_min
            print(f"   Prueba ANGLE_OFFSET_DEG = {suggested_offset:.1f}")
    
    print("="*70 + "\n")
    
    return len(sector_points) > 0


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
            if frame:  # Solo devolver si hay puntos
                yield frame
            frame = []
        
        prev_angle = a_corr
        frame.append({"a_corr": a_corr, "d": d, "q": q})


async def build_baseline(lidar: RPLidar, seconds: float):
    per_bin = defaultdict(list)
    t_start = time.time()
    t_end = t_start + seconds
    
    print("  Recopilando datos de fondo...")
    frame_count = 0
    points_collected = 0
    last_update = t_start
    
    async for frame in read_frames_by_revolution(lidar):
        frame_count += 1
        points_in_frame = len(frame)
        
        for p in frame:
            per_bin[bin_angle(p["a_corr"])].append(p["d"])
            points_collected += 1
        
        # Actualizar cada 0.5s
        now = time.time()
        if now - last_update >= 0.5:
            elapsed = now - t_start
            print(f"    {elapsed:.1f}s | Frames: {frame_count} | Puntos: {points_collected}")
            last_update = now
        
        if now >= t_end:
            break
    
    baseline = {}
    for b, ds in per_bin.items():
        if ds:
            baseline[b] = median(ds)
    
    print(f"\n  ✅ Baseline: {len(baseline)} bins, {points_collected} puntos")
    
    if baseline:
        dists = list(baseline.values())
        print(f"     Dist: {min(dists):.0f}-{max(dists):.0f}mm (med: {median(dists):.0f}mm)")
    
    return baseline


# =========================
# MAIN
# =========================
async def main():
    lidar = RPLidar(PORT, BAUDRATE)
    
    try:
        print("="*70)
        print("DETECTOR DE PELOTA - LIDAR")
        print("="*70)
        print(f"\nHealth check: {lidar.healthcheck()}")
        
        lidar.stop_event.clear()
        
        async with asyncio.TaskGroup() as tg:
            tg.create_task(lidar.simple_scan(make_return_dict=True))
            
            # DIAGNÓSTICO PRIMERO
            if DIAGNOSTIC_MODE:
                await asyncio.sleep(0.5)  # Esperar a que el LIDAR arranque
                has_data = await diagnostic_scan(lidar, duration=3.0)
                
                if not has_data:
                    print("\n❌ No se detectaron puntos en el sector.")
                    print("   Revisa el ANGLE_OFFSET_DEG o la orientación del LIDAR.")
                    print("\n💡 ¿Quieres continuar de todos modos? (s/n): ", end='')
                    return
            
            # Configuración normal
            print(f"\n📐 CONFIGURACIÓN:")
            print(f"  Área: {LAT_MAX_M}m x {HEIGHT_MAX_M}m")
            print(f"  Sector: {ANGLE_MIN}°-{ANGLE_MAX}° (offset={ANGLE_OFFSET_DEG}°)")
            
            print(f"\n🔄 Construyendo baseline ({BASELINE_SECONDS:.1f}s)...")
            baseline = await build_baseline(lidar, BASELINE_SECONDS)
            
            if not baseline:
                print("\n❌ ERROR: No se pudo crear baseline. No hay datos en el sector.")
                return
            
            print(f"\n🎯 Detectando pelota... (Ctrl+C para salir)\n")
            
            last_event_ts = 0.0
            frame_count = 0
            event_count = 0
            
            async for frame in read_frames_by_revolution(lidar):
                frame_count += 1
                candidates = []
                
                for p in frame:
                    b = bin_angle(p["a_corr"])
                    base_d = baseline.get(b)
                    
                    if base_d is None:
                        continue
                    
                    diff = base_d - p["d"]
                    
                    if diff >= DELTA_MM:
                        lat_m, h_m = polar_to_wall_coords(p["a_corr"], p["d"])
                        
                        if lat_m < 0 or h_m < 0:
                            continue
                        if lat_m > LAT_MAX_M or h_m > HEIGHT_MAX_M:
                            continue
                        
                        candidates.append({
                            'lat': lat_m,
                            'h': h_m,
                            'angle': p["a_corr"],
                            'dist': p["d"],
                            'diff': diff
                        })
                        
                        baseline[b] = BASELINE_ALPHA * baseline[b] + (1 - BASELINE_ALPHA) * p["d"]
                
                now = time.time()
                if len(candidates) >= MIN_POINTS_EVENT and (now - last_event_ts) >= EVENT_COOLDOWN_S:
                    best = min(candidates, key=lambda c: c['dist'])
                    lat01, h01 = normalize_coords(best['lat'], best['h'])
                    
                    atomic_overwrite(OUT_FILE, f"{lat01:.4f},{h01:.4f}\n")
                    last_event_ts = now
                    event_count += 1
                    
                    print(f"\n⚽ PELOTA #{event_count}: lat={lat01:.4f} h={h01:.4f} "
                          f"({best['lat']:.2f}m, {best['h']:.2f}m) | {len(candidates)} puntos")
    
    except KeyboardInterrupt:
        print("\n\n⏹️  Deteniendo...")
        lidar.stop_event.set()
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        lidar.reset()
        print("\n✅ Reset completado.")


if __name__ == "__main__":
    asyncio.run(main())
