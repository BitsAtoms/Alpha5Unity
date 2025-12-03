import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import os
import time

# ==============================
# CONFIGURACIÓN
# ==============================

BASE_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUTPUT_PATH = os.path.join(BASE_PATH, "Tracking", "AnimationFile_Side.txt")
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 0
BALL_CLASS_ID = 32

# ==============================
# USAR CPU 
# ==============================
device = "cpu"
print("Usando:", device)

# Modelo super ligero
MODEL_PATH = os.path.join(os.path.dirname(__file__), "yolo11n.pt")
model = YOLO(MODEL_PATH).to(device)

# ==============================
# INICIALIZAR CÁMARA 
# ==============================

cap = cv2.VideoCapture(CAM_INDEX, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_BUFFERSIZE, 0)
cap.set(cv2.CAP_PROP_FPS, 30)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

# ==============================
# LOOP
# ==============================

last_write = time.time()
write_interval = 0.02  # escribir 50 veces por segundo

y_position = "0.0000"

while True:

    ret, frame = cap.read()
    if not ret:
        continue

    # YOLO en CPU → muy estable
    results = model(frame, verbose=False)[0]

    h, w, _ = frame.shape

    for box in results.boxes:
        if int(box.cls[0]) == BALL_CLASS_ID:

            x1, y1, x2, y2 = map(int, box.xyxy[0])
            cx = (x1 + x2) // 2
            cy = (y1 + y2) // 2

            # Normalización Y
            y_norm = 1 - (cy / h)
            y_norm = np.clip(y_norm, 0.0, 1.0)
            y_position = f"{y_norm:.4f}"

            # (Opcional para debug)
            cvzone.cornerRect(frame, (x1, y1, x2 - x1, y2 - y1))
            cvzone.putTextRect(frame, "Ball", (x1, y1 - 10))
            break

    # Escritura sin bloquear el sistema
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