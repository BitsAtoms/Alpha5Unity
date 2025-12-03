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
BASE_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUTPUT_PATH = os.path.join(BASE_PATH, "Tracking", "AnimationFile_Side.txt")
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 0   # <- CORRECTO SEGÚN TU SCRIPT ORIGINAL
BALL_CLASS_ID = 32
MODEL_PATH = os.path.join(os.path.dirname(__file__), "yolo11n.pt")

model = YOLO(MODEL_PATH).to(device)

cap = cv2.VideoCapture(CAM_INDEX, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_FPS, 30)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

tracker = None
tracking_active = False
last_seen = time.time()

write_interval = 0.02
last_write = time.time()

y_position = "0.5000"

recent_detections = []
min_frames_confirm = 3

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

    # ======================
    # YOLO si no hay tracker
    # ======================
    if not tracking_active:

        results = model(frame, verbose=False)[0]

        candidate = None
        best_area = 0

        for box in results.boxes:
            if int(box.cls[0]) != BALL_CLASS_ID:
                continue

            x1, y1, x2, y2 = map(int, box.xyxy[0])

            if not is_ball_shape(x1, y1, x2, y2):
                continue

            area = (x2 - x1) * (y2 - y1)
            if area < 200:
                continue

            if area > best_area:
                best_area = area
                candidate = (x1, y1, x2, y2)

        if candidate:
            recent_detections.append(candidate)

            if len(recent_detections) > min_frames_confirm:
                recent_detections.pop(0)

            if len(recent_detections) == min_frames_confirm:
                x1, y1, x2, y2 = recent_detections[-1]
                tracker = cv2.TrackerCSRT_create()
                tracker.init(frame, (x1, y1, x2-x1, y2-y1))
                tracking_active = True
                last_seen = time.time()
                recent_detections.clear()

    # ======================
    # TRACKER ACTIVO
    # ======================
    else:
        success, bbox = tracker.update(frame)

        if success:
            x, y, wbox, hbox = [int(v) for v in bbox]

            cy = y + hbox // 2

            y_norm = np.clip(1 - (cy / h), 0, 1)
            y_position = f"{y_norm:.4f}"

            last_seen = time.time()

            cv2.rectangle(frame, (x, y), (x+wbox, y+hbox), (0,255,0), 2)

        else:
            tracking_active = False
            tracker = None

        if time.time() - last_seen > 0.4:
            tracking_active = False
            tracker = None

    # ======================
    # ESCRIBIR 
    # ======================
    if time.time() - last_write >= write_interval:
        try:
            with open(OUTPUT_PATH, "w") as f:
                f.write(y_position + "\n")
        except:
            pass
        last_write = time.time()

    cv2.imshow("Side Camera", frame)
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()
cv2.destroyAllWindows()