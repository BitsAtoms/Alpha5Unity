import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import os

# ==============================
# CONFIGURACIÓN
# ==============================

OUTPUT_PATH = r"C:\\Tracking\\AnimationFile_Top.txt"
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 0                 # la cámara del techo

BALL_CLASS_ID = 32            # AJUSTA si tu YOLO detecta otro ID

device = "cuda" if torch.cuda.is_available() else "cpu"
print("Usando:", device)
model = YOLO("yolo11x.pt").to(device)

# ==============================
# CÁMARA
# ==============================

cap = cv2.VideoCapture(CAM_INDEX)

position = "0.0,0.0"   # valor por defecto

# ==============================
# LOOP PRINCIPAL
# ==============================

while True:

    ret, frame = cap.read()
    if not ret:
        continue

    results = model(frame, device=device, verbose=False)

    cx_norm, cz_norm = None, None

    for r in results:
        for box in r.boxes:
            cls = int(box.cls[0])
            if cls == BALL_CLASS_ID:

                # Extraer bounding box
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx = x1 + (x2 - x1)//2
                cy = y1 + (y2 - y1)//2

                cvzone.cornerRect(frame, (x1, y1, x2-x1, y2-y1))
                cvzone.putTextRect(frame, "Ball", (x1, y1-10))

                # ============================
                # NORMALIZACIÓN XZ PARA UNITY
                # ============================

                h, w, _ = frame.shape
                cx_norm = cx / w
                cz_norm = 1 - (cy / h)
                cx_norm = max(0.0, min(1.0, cx_norm))
                cz_norm = max(0.0, min(1.0, cz_norm))



                position = f"{cx_norm:.4f},{cz_norm:.4f}"

    # escribir archivo
    print(position)
    try:
        with open(OUTPUT_PATH, "w") as f:
            f.write(position + "\n")
    except PermissionError:
        pass

    cv2.imshow("Top Camera", frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
