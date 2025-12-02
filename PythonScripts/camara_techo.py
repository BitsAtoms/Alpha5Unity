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
OUTPUT_PATH = os.path.join(BASE_PATH, "Tracking", "AnimationFile_Top.txt")
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 1
BALL_CLASS_ID = 32

# ==============================
# USAR CPU (MUY IMPORTANTE)
# ==============================
device = "cpu"
print("Usando:", device)

# Modelo super ligero
MODEL_PATH = os.path.join(os.path.dirname(__file__), "yolo11n.pt")
model = YOLO(MODEL_PATH).to(device)

# ==============================
# INICIALIZAR CÁMARA (OPTIMIZADA)
# ==============================

cap = cv2.VideoCapture(CAM_INDEX, cv2.CAP_DSHOW)
cap.set(cv2.CAP_PROP_BUFFERSIZE, 0)
cap.set(cv2.CAP_PROP_FPS, 30)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

last_write = time.time()
write_interval = 0.02  # 50 FPS reales

position = "0.0000,0.0000"

# ==============================
# LOOP
# ==============================

while True:

    ret, frame = cap.read()
    if not ret:
        continue

    results = model(frame, verbose=False)[0]
    h, w, _ = frame.shape

    for box in results.boxes:
        if int(box.cls[0]) == BALL_CLASS_ID:

            x1, y1, x2, y2 = map(int, box.xyxy[0])
            cx = (x1 + x2) // 2
            cy = (y1 + y2) // 2

            # Normalizar XZ
            cx_norm = cx / w
            cz_norm = 1 - (cy / h)

            cx_norm = np.clip(cx_norm, 0.0, 1.0)
            cz_norm = np.clip(cz_norm, 0.0, 1.0)

            position = f"{cx_norm:.4f},{cz_norm:.4f}"

            # (Opcional)
            cvzone.cornerRect(frame, (x1, y1, x2 - x1, y2 - y1))
            cvzone.putTextRect(frame, "Ball", (x1, y1 - 10))
            break

    # Escritura regulada
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