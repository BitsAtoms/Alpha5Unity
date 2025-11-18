import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import math

# ============================
# CONFIGURACIÓN CÁMARAS / ESTÉREO
# ============================

CAM_LEFT_INDEX = 0
CAM_RIGHT_INDEX = 1

FRAME_WIDTH = 640
FRAME_HEIGHT = 360

BASELINE_METERS = 0.40 # distancia entre las dos cámaras

# APROXIMACIÓN: puedes empezar en 800, luego ajustar con Z_SCALE
FOCAL_PIXELS = 800.0

# 🔧 FACTOR DE ESCALA PARA AJUSTAR Z A TUS MEDIDAS REALES
# Haz la prueba: coloca la pelota a 3 m, mira qué Z te da, y pon Z_SCALE = 3 / Z_medio
Z_SCALE = 0.20   # EJEMPLO: si el programa dice ~30m cuando realmente son 3m → 3/30 = 0.1

# ============================
# CONFIG YOLO
# ============================

BALL_CLASS_ID = 32

device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Usando dispositivo: {device}")

model = YOLO('yolo11x.pt')
model.to(device)

# ============================
# CÁMARAS
# ============================

cap_left = cv2.VideoCapture(CAM_LEFT_INDEX)
cap_right = cv2.VideoCapture(CAM_RIGHT_INDEX)

if not cap_left.isOpened():
    raise IOError("❌ No se pudo abrir la cámara izquierda.")
if not cap_right.isOpened():
    raise IOError("❌ No se pudo abrir la cámara derecha.")

cap_left.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap_left.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)
cap_right.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap_right.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)

print("🎥 Cámaras inicializadas.")

output_file = open("StereoCoords.txt", "w", encoding="utf-8")
print("📂 Guardando coordenadas 3D en StereoCoords.txt")


def detectar_pelota(img, min_conf=0.35):
    """
    Devuelve:
      - best_center: (cx, cy) o None
      - best_box: (x1, y1, x2, y2) o None
      - best_conf: confianza
    Solo acepta detecciones con confianza >= min_conf.
    """

    # Filtramos directamente la clase BALL_CLASS_ID (pelota)
    results = model(
        img,
        device=device,
        stream=False,
        verbose=False,
        classes=[BALL_CLASS_ID]   # 👉 SOLO la clase pelota
    )

    best_conf = 0.0
    best_center = None
    best_box = None

    for r in results:
        for box in r.boxes:
            conf = float(box.conf[0])
            if conf >= min_conf:            # filtro de confianza mínima para que detecte mejor la pelota
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx = x1 + (x2 - x1) // 2
                cy = y1 + (y2 - y1) // 2

                if conf > best_conf:
                    best_conf = conf
                    best_center = (cx, cy)
                    best_box = (x1, y1, x2, y2)

    return best_center, best_box, best_conf



while True:
    retL, frame_left = cap_left.read()
    retR, frame_right = cap_right.read()

    if not retL or not retR:
        print("⚠️ Error al leer una de las cámaras.")
        continue

    frame_left = cv2.resize(frame_left, (FRAME_WIDTH, FRAME_HEIGHT))
    frame_right = cv2.resize(frame_right, (FRAME_WIDTH, FRAME_HEIGHT))

    centerL, boxL, confL = detectar_pelota(frame_left, min_conf=0.35) #ajusta la min conf para la detección
    centerR, boxR, confR = detectar_pelota(frame_right, min_conf=0.35)

    if boxL is not None:
        x1, y1, x2, y2 = boxL
        cvzone.cornerRect(frame_left, (x1, y1, x2 - x1, y2 - y1), colorR=(0, 255, 255))
        cvzone.putTextRect(frame_left, f"Ball {confL:.2f}", (x1, y1 - 10),
                           colorR=(0, 255, 255), colorT = (0, 0, 0), scale=1, thickness=1)

    if boxR is not None:
        x1, y1, x2, y2 = boxR
        cvzone.cornerRect(frame_right, (x1, y1, x2 - x1, y2 - y1), colorR=(0, 255, 255))
        cvzone.putTextRect(frame_right, f"Ball {confR:.2f}", (x1, y1 - 10),
                           colorR=(0, 255, 255), colorT = (0, 0, 0), scale=1, thickness=1)

    if (centerL is not None) and (centerR is not None):
        cxL, cyL = centerL
        cxR, cyR = centerR

        disparity = float(cxL - cxR)

        if disparity != 0:
            # 1) PROFUNDIDAD BASE SIN ESCALA
            Z_calc = (FOCAL_PIXELS * BASELINE_METERS) / abs(disparity)

            # 2) APLICAR FACTOR DE ESCALA PARA AJUSTAR A TU MUNDO REAL
            Z = Z_calc * Z_SCALE

            # 3) CÁLCULO DE X, Y (centro de la imagen como origen)
            cx0 = FRAME_WIDTH / 2.0
            cy0 = FRAME_HEIGHT / 2.0

            x_norm = (cxL - cx0) / FOCAL_PIXELS
            y_norm = (cyL - cy0) / FOCAL_PIXELS

            # 👉 Si quieres X>0 a la derecha, Y>0 hacia arriba:
            X = x_norm * Z
            Y = -y_norm * Z   # signo menos para invertir eje vertical

            cvzone.putTextRect(
                frame_left,
                f"X={X:.2f}m Y={Y:.2f}m Z={Z:.2f}m",
                (30, 30),
                scale=1,
                thickness=1,
                colorR=(0, 255, 0),
                colorT = (0, 0, 0)
            )

            line = f"{X:.4f},{Y:.4f},{Z:.4f}"
            print(line)
            output_file.write(line + "\n")
            output_file.flush()
        else:
            cvzone.putTextRect(
                frame_left,
                "Disparidad 0: no hay profundidad",
                (30, 30),
                scale=1,
                thickness=1,
                colorR=(0, 0, 255), 
                colorT = (0, 0, 0)
            )
    else:
        cvzone.putTextRect(
            frame_left,
            "Esperando pelota en ambas cámaras...",
            (30, 30),
            scale=1,
            thickness=1,
            colorR=(0, 0, 255), 
            colorT = (0, 0, 0)
        )

    cv2.imshow("Camara Izquierda", frame_left)
    cv2.imshow("Camara Derecha", frame_right)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap_left.release()
cap_right.release()
cv2.destroyAllWindows()
output_file.close()
print("✅ Cámaras cerradas y archivo StereoCoords.txt guardado.")
