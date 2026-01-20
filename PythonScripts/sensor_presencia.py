import serial
import time
import os

PORT = "COM4"
BAUDRATE = 115200

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_FILE = os.path.join(BASE_DIR, "Config", "presencia.txt")
os.makedirs(os.path.dirname(OUT_FILE), exist_ok=True)


# Si no llega NADA por el puerto en este tiempo -> escribe 0
INACTIVE_TIMEOUT = 0.6  # segundos (ajústalo a tu gusto)


def write_value(path: str, value: str) -> None:
    with open(path, "w", encoding="utf-8") as f:
        f.write(value)


def read_value(path: str) -> str | None:
    if not os.path.exists(path):
        return None
    try:
        with open(path, "r", encoding="utf-8") as f:
            v = f.read(1)
        return v if v in ("0", "1") else None
    except Exception:
        return None


def main():
    last_written = read_value(OUT_FILE)  # para no escribir repetido
    last_data_time = time.time()

    try:
        ser = serial.Serial(PORT, BAUDRATE, timeout=0.1)
        time.sleep(2)
        print(f"Leyendo de {PORT} @ {BAUDRATE}...")

        while True:
            data = ser.read(ser.in_waiting or 1)

            now = time.time()

            # 1) Si llega CUALQUIER DATO -> presencia = 1
            if data:
                last_data_time = now
                if last_written != "1":
                    write_value(OUT_FILE, "1")
                    last_written = "1"
                    print("Dato recibido -> escrito 1")

            # 2) Si NO llega nada durante X segundos -> presencia = 0
            if INACTIVE_TIMEOUT is not None and (now - last_data_time) >= INACTIVE_TIMEOUT:
                if last_written != "0":
                    write_value(OUT_FILE, "0")
                    last_written = "0"
                    print("Sin datos -> escrito 0")

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
