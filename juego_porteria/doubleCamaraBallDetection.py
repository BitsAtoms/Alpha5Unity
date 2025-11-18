import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import math
import os

# ============================
# CONFIGURACIÓN CÁMARAS / ESTÉREO
# ============================

CAM_LEFT_INDEX = 0
CAM_RIGHT_INDEX = 1

FRAME_WIDTH = 640
FRAME_HEIGHT = 360

BASELINE_METERS = 0.40  # Distancia entre cámaras
FILE_PATH = r"C:\\Tracking\\AnimationFile.txt"
os.makedirs(os.path.dirname(FILE_PATH), exist_ok=True)

FOCAL_PIXELS = 800.0
Z_SCALE = 0.20   # Ajustable según pruebas

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

# Abrir archivo
output_file = open(FILE_PATH, "w", encoding="utf-8")
print("📂 Guardando coordenadas 3D en AnimationFile.txt")


# ============================
# DETECTOR DE PELOTA
# ============================

def detectar_pelota(img, min_conf=0.35):

    results = model(
        img,
        device=device,
        stream=False,
        verbose=False,
        classes=[BALL_CLASS_ID]
    )

    best_conf = 0.0
    best_center = None
    best_box = None

    for r in results:
        for box in r.boxes:
            conf = float(box.conf[0])
            if conf >= min_conf:
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx = x1 + (x2 - x1) // 2
                cy = y1 + (y2 - y1) // 2

                if conf > best_conf:
                    best_conf = conf
                    best_center = (cx, cy)
                    best_box = (x1, y1, x2, y2)

    return best_center, best_box, best_conf


# ============================
# LOOP PRINCIPAL
# ============================

while True:

    retL, frame_left = cap_left.read()
    retR, frame_right = cap_right.read()

    if not retL or not retR:
        print("⚠️ Error al leer una de las cámaras.")
        continue

    frame_left = cv2.resize(frame_left, (FRAME_WIDTH, FRAME_HEIGHT))
    frame_right = cv2.resize(frame_right, (FRAME_WIDTH, FRAME_HEIGHT))

    centerL, boxL, confL = detectar_pelota(frame_left)
    centerR, boxR, confR = detectar_pelota(frame_right)

    # Dibujar cajas si existen
    if boxL is not None:
        x1, y1, x2, y2 = boxL
        cvzone.cornerRect(frame_left, (x1, y1, x2 - x1, y2 - y1))
        cvzone.putTextRect(frame_left, f"Ball {confL:.2f}", (x1, y1 - 10))

    if boxR is not None:
        x1, y1, x2, y2 = boxR
        cvzone.cornerRect(frame_right, (x1, y1, x2 - x1, y2 - y1))
        cvzone.putTextRect(frame_right, f"Ball {confR:.2f}", (x1, y1 - 10))

    # ============================
    # CÁLCULO 3D SI HAY PELOTA EN AMBAS CÁMARAS
    # ============================

    if (centerL is not None) and (centerR is not None):

        cxL, cyL = centerL
        cxR, cyR = centerR
        disparity = float(cxL - cxR)

        if disparity != 0:

            # PROFUNDIDAD BASE
            Z_calc = (FOCAL_PIXELS * BASELINE_METERS) / abs(disparity)
            Z = Z_calc * Z_SCALE

            # Coordenadas centradas
            cx0 = FRAME_WIDTH / 2.0
            cy0 = FRAME_HEIGHT / 2.0

            x_norm = (cxL - cx0) / FOCAL_PIXELS
            y_norm = (cyL - cy0) / FOCAL_PIXELS

            X = x_norm * Z
            Y = -y_norm * Z

            # Mostrar en pantalla
            cvzone.putTextRect(
                frame_left,
                f"X={X:.2f} Y={Y:.2f} Z={Z:.2f}",
                (30, 30)
            )

            # ESCRIBIR POSICIÓN REAL
            try:
                with open(FILE_PATH, "w") as f:
                    f.write(f"{X:.4f},{Y:.4f},{Z:.4f}\n")
            except:
                pass
            

    else:
        # ==== NO HAY PELOTA 3D ====
        

        cvzone.putTextRect(
            frame_left,
            "Esperando pelota en ambas cámaras...",
            (30, 30)
        )

    # Mostrar cámaras
    cv2.imshow("Camara Izquierda", frame_left)
    cv2.imshow("Camara Derecha", frame_right)

    # Cerrar con tecla Q
    if cv2.waitKey(1) & 0xFF == ord('q'):
        try:
            with open(FILE_PATH, "w") as f:
                f.write("0,0,0\n")
        except:
            pass
        break

# Cerrar cámaras
cap_left.release()
cap_right.release()
cv2.destroyAllWindows()
output_file.close()
print("✅ Cámaras cerradas y AnimationFile.txt reiniciado.")
