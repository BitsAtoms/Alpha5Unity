import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import os

# ============================
# CONFIGURACIÓN
# ============================

OUTPUT_PATH = r"C:\\Tracking\\AnimationFile.txt"
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

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
    best = None
    for r in results:
        for box in r.boxes:
            if int(box.cls[0]) == BALL_CLASS_ID:
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx = x1 + (x2 - x1) // 2
                cy = y1 + (y2 - y1) // 2
                best = (cx, cy, x1, y1, x2, y2)
    return best


# ============================
# LOOP PRINCIPAL
# ============================

last_valid = "0.5000,0.5000,0.0000"   # Centro por defecto

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
        cxR, cyR, x1R, y1R, x2R, y2R = detR

        # Dibujar cajas
        cvzone.cornerRect(frameL, (x1L, y1L, x2L - x1L, y2L - y1L))
        cvzone.cornerRect(frameR, (x1R, y1R, x2R - x1R, y2R - y1R))

        # ============================
        # NORMALIZACIÓN (MISMA LÓGICA QUE 1 CÁMARA)
        # ============================

        # Usamos la posición de la cámara izquierda como referencia final
        cx_norm = cyL / FRAME_H        # 0 arriba → 1 abajo
        cy_norm = cxL / FRAME_W        # 0 izquierda → 1 derecha
        cy_norm = 1 - cy_norm          # invertir eje X para Unity

        salida = f"{cx_norm:.4f},{cy_norm:.4f},0.0000"
        last_valid = salida

    else:
        # Sin detección → mantener última buena posición
        salida = last_valid

    # ============================
    # GUARDAR EN ARCHIVO
    # ============================

    try:
        with open(OUTPUT_PATH, "w") as f:
            f.write(salida + "\n")
    except:
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
