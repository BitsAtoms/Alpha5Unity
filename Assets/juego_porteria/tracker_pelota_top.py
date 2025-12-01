import cv2
import numpy as np
from ultralytics import YOLO
import torch
import time
import os

# ======================
# DETECTAR GPU o CPU
# ======================
def get_device():
    return "cuda" if torch.cuda.is_available() else "cpu"

device = get_device()
print("Usando:", device)

# ======================
# CONFIG
# ======================
OUTPUT_PATH = r"C:\\Tracking\\AnimationFile_Top.txt"
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 1
BALL_CLASS_ID = 32
model = YOLO("yolo11n.pt").to(device)

cap = cv2.VideoCapture(CAM_INDEX, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_FPS, 30)

tracker = None
tracking_active = False
last_seen = time.time()

write_interval = 0.02
last_write = time.time()

position = "0.5000,0.5000"

# memoria de detecciones
recent_detections = []
min_frames_confirm = 3   # debe aparecer en 3 frames seguidos

def is_ball_shape(x1, y1, x2, y2):
    w = x2 - x1
    h = y2 - y1

    if w < 15 or h < 15:  
        return False

    aspect = w / float(h)
    return 0.75 < aspect < 1.25 

while True:
    ret, frame = cap.read()
    if not ret:
        continue

    h, w, _ = frame.shape

    # ==========================
    # YOLO si no hay tracker
    # ==========================
    if not tracking_active:

        results = model(frame, verbose=False)[0]

        candidate = None
        best_area = 0

        for box in results.boxes:
            if int(box.cls[0]) != BALL_CLASS_ID:
                continue

            x1, y1, x2, y2 = map(int, box.xyxy[0])

            # FILTRO 1: forma cercana a círculo
            if not is_ball_shape(x1, y1, x2, y2):
                continue

            area = (x2 - x1) * (y2 - y1)

            # solo quedarse con detecciones serias
            if area < 200:
                continue

            # queda la más grande
            if area > best_area:
                best_area = area
                candidate = (x1, y1, x2, y2)

        # si no hay nada claro → no arranco tracker aún
        if candidate:
            recent_detections.append(candidate)

            # mantener memoria corta
            if len(recent_detections) > min_frames_confirm:
                recent_detections.pop(0)

            # comprobar consistencia temporal
            if len(recent_detections) == min_frames_confirm:
                # confirmar pelota real
                x1, y1, x2, y2 = recent_detections[-1]
                tracker = cv2.TrackerCSRT_create()
                tracker.init(frame, (x1, y1, x2-x1, y2-y1))
                tracking_active = True
                last_seen = time.time()
                recent_detections.clear()

    # ==========================
    # TRACKER ACTIVO
    # ==========================
    else:
        success, bbox = tracker.update(frame)

        if success:
            x, y, wbox, hbox = [int(v) for v in bbox]

            cx = x + wbox // 2
            cy = y + hbox // 2

            cx_norm = np.clip(cx / w, 0, 1)
            cz_norm = np.clip(1 - (cy / h), 0, 1)

            position = f"{cx_norm:.4f},{cz_norm:.4f}"
            last_seen = time.time()

            cv2.rectangle(frame, (x, y), (x+wbox, y+hbox), (0,255,0), 2)

        else:
            tracking_active = False
            tracker = None

        if time.time() - last_seen > 0.4:
            tracking_active = False
            tracker = None

    # ==========================
    # ESCRIBIR
    # ==========================
    if time.time() - last_write >= write_interval:
        try:
            with open(OUTPUT_PATH, "w") as f:
                f.write(position + "\n")
        except:
            pass
        last_write = time.time()


    cv2.imshow("Top Camera", frame)
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()
cv2.destroyAllWindows()