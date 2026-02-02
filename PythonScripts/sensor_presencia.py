import serial
import time
import os

PORT = "COM3"
BAUDRATE = 115200

# Si NO llega detección válida durante este tiempo, se rearma para permitir otro timestamp
INACTIVE_TIMEOUT = 0.6  # segundos

DEBUG_PRINT_LINES = True  # ponlo en False si no quieres ver cada mensaje

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

def ts_ms():
    return int(time.time() * 1000)

def atomic_overwrite(path: str, text: str):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        f.write(text)
    os.replace(tmp, path)

def write_keeper_move(path: str):
    # Ahora el archivo SOLO contiene el timestamp
    atomic_overwrite(path, f"{ts_ms()}\n")

# =========================
# RUTAS
# =========================
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = find_project_root(BASE_DIR, "Alpha5Unity")
CONFIG_DIR = os.path.join(PROJECT_ROOT, "Config")
os.makedirs(CONFIG_DIR, exist_ok=True)

KEEPER_MOVE_FILE = os.path.join(CONFIG_DIR, "keeper_move.txt")

# =========================
# PARSEO / FILTRO
# =========================
def is_valid_message(msg: str) -> bool:
    """
    Reglas:
    - Ignorar vacío
    - Ignorar cualquier mensaje que contenga 'XX' (fuera de rango) en cualquier parte:
      Ej: 'XX', 'Dz=XX', 'X002B[Dz=XX]', etc.
    - El resto se considera "detección válida"
    """
    if not msg:
        return False

    up = msg.strip().upper()
    if not up:
        return False

    # ✅ CLAVE: si contiene XX en cualquier parte => inválido
    if "XX" in up:
        return False

    return True

# =========================
# MAIN
# =========================
def main():
    print("[PRESENCIA] Config dir:", CONFIG_DIR)
    print("[PRESENCIA] keeper_move:", KEEPER_MOVE_FILE)

    triggered = False
    last_valid_time = 0.0

    try:
        ser = serial.Serial(PORT, BAUDRATE, timeout=0.2)
        time.sleep(2)
        ser.reset_input_buffer()
        print(f"Leyendo de {PORT} @ {BAUDRATE}...")

        while True:
            raw = ser.readline()  # leemos líneas completas

            if raw:
                msg = raw.decode("utf-8", errors="ignore").strip()

                if DEBUG_PRINT_LINES:
                    print("[SER]", msg)

                if is_valid_message(msg):
                    # ✅ detección válida
                    last_valid_time = time.time()

                    # ✅ escribir SOLO UNA VEZ por bloque de detección
                    if not triggered:
                        write_keeper_move(KEEPER_MOVE_FILE)
                        triggered = True
                        print("Detección válida -> escrito TIMESTAMP")

            # ✅ rearme cuando lleva tiempo sin detección válida
            if triggered and (time.time() - last_valid_time) >= INACTIVE_TIMEOUT:
                triggered = False
                print("Rearmado: listo para escribir otro timestamp")

            time.sleep(0.01)

    except serial.SerialException as e:
        print(f"No se pudo abrir {PORT}: {e}")
    except KeyboardInterrupt:
        print("\nCerrando...")
    finally:
        try:
            if "ser" in locals() and ser and ser.is_open:
                ser.close()
        except Exception:
            pass

if __name__ == "__main__":
    main()
