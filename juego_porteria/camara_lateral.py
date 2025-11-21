import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import os

# ==============================
# CONFIGURACIÓN
# ==============================

OUTPUT_PATH = r"C:\\Tracking\\AnimationFile_Side.txt"
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

CAM_INDEX = 1             # la cámara lateral (ajusta si hace falta)

BALL_CLASS_ID = 32        # Ajustar si YOLO devuelve otro ID

device = "cuda" if torch.cuda.is_available() else "cpu"
print("Usando:", device)
model = YOLO("yolo11x.pt").to(device)

# ==============================
# CÁMARA
# ==============================

cap = cv2.VideoCapture(CAM_INDEX)

y_position = "0.0000"  # valor por defecto

# ==============================
# LOOP PRINCIPAL
# ==============================

while True:

    ret, frame = cap.read()
    if not ret:
        continue

    results = model(frame, device=device, verbose=False)

    y_norm = None

    for r in results:
        for box in r.boxes:
            cls = int(box.cls[0])
            if cls == BALL_CLASS_ID:

                # Bounding box
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx = x1 + (x2 - x1)//2
                cy = y1 + (y2 - y1)//2

                cvzone.cornerRect(frame, (x1, y1, x2-x1, y2-y1))
                cvzone.putTextRect(frame, "Ball", (x1, y1-10))

                # ============================
                # NORMALIZACIÓN Y PARA UNITY
                # ============================
                # cy = 0 (arriba) → pelota está alta
                # cy = FRAME_H → pelota está baja
                # Queremos 0 = suelo, 1 = arriba
                h, w, _ = frame.shape
                y_norm = 1 - (cy / h)

                # ⛔️ evitar valores negativos o >1
                y_norm = np.clip(y_norm, 0.0, 1.0)

                y_position = f"{y_norm:.4f}"

    # Guardar en archivo de intercambio
    print(y_position)
    try:
        with open(OUTPUT_PATH, "w") as f:
            f.write(y_position + "\n")
    except PermissionError:
        pass

    cv2.imshow("Side Camera", frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
