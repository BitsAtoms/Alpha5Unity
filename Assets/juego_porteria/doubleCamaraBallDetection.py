import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import os

# ============================
# CONFIGURACIÓN
# ============================

OUT_TOP = r"C:\\Tracking\\AnimationFile_Top.txt"    # X, Z
OUT_SIDE = r"C:\\Tracking\\AnimationFile_Side.txt"  # Y (por ahora 0)

os.makedirs(os.path.dirname(OUT_TOP), exist_ok=True)

CAM_LEFT = 0
CAM_RIGHT = 1
FRAME_W = 640
FRAME_H = 360

BALL_CLASS_ID = 32

device = "cuda" if torch.cuda.is_available() else "cpu"
print("Usando:", device)

model = YOLO("yolo11x.pt")
model.to(device)

# ============================
# INICIAR CÁMARAS
# ============================

capL = cv2.VideoCapture(CAM_LEFT)
capR = cv2.VideoCapture(CAM_RIGHT)

capL.set(3, FRAME_W)
capL.set(4, FRAME_H)
capR.set(3, FRAME_W)
capR.set(4, FRAME_H)

if not capL.isOpened() or not capR.isOpened():
    raise IOError("❌ No se pudieron abrir ambas cámaras.")


# ============================
# FUNCIÓN DETECTAR PELOTA
# ============================

def detectar(img):
    results = model(img, verbose=False, device=device, stream=False)
    best_area = 0
    best = None

    for r in results:
        for box in r.boxes:
            if int(box.cls[0]) == BALL_CLASS_ID:
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                area = (x2 - x1) * (y2 - y1)
                if area > best_area:
                    best_area = area
                    cx = x1 + (x2 - x1) // 2
                    cy = y1 + (y2 - y1) // 2
                    best = (cx, cy, x1, y1, x2, y2)

    return best


# ============================
# LOOP PRINCIPAL
# ============================

# Valores iniciales seguros (centro del campo)
last_top = "0.5000,0.5000"
last_side = "0.0000"  # altura por defecto

while True:

    retL, frameL = capL.read()
    retR, frameR = capR.read()

    if not retL or not retR:
        continue

    frameL = cv2.resize(frameL, (FRAME_W, FRAME_H))
    frameR = cv2.resize(frameR, (FRAME_W, FRAME_H))

    detL = detectar(frameL)
    detR = detectar(frameR)

    # ============================
    # SI AMBAS CÁMARAS DETECTAN LA PELOTA
    # ============================

    if detL and detR:

        cxL, cyL, x1L, y1L, x2L, y2L = detL

        # ============================
        # NORMALIZACIÓN X,Z PARA UNITY
        # ============================

        cx_norm = cyL / FRAME_H       # Y → X
        cz_norm = cxL / FRAME_W       # X → Z
        cz_norm = 1 - cz_norm         # invertir eje horizontal

        salida_top = f"{cx_norm:.4f},{cz_norm:.4f}"
        last_top = salida_top

        # ============================
        # ALTURA (Y) — de momento fija
        # ============================

        salida_side = "0.0000"
        last_side = salida_side

    else:
        salida_top = last_top
        salida_side = last_side

    # ============================
    # GUARDAR LOS 2 ARCHIVOS
    # ============================

    try:
        with open(OUT_TOP, "w") as f:
            f.write(salida_top)

        with open(OUT_SIDE, "w") as f:
            f.write(salida_side)

    except PermissionError:
        pass

    # Mostrar cámaras
    cv2.imshow("Camara Izquierda", frameL)
    cv2.imshow("Camara Derecha", frameR)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break


capL.release()
capR.release()
cv2.destroyAllWindows()
print("Cámaras cerradas.")
