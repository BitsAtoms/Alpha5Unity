import cv2
import numpy as np
from ultralytics import YOLO
import cvzone
import torch
import os

# Ruta donde se guardará el archivo para Unity
OUTPUT_PATH = r"C:\Tracking\AnimationFile.txt" 
os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)

cap = cv2.VideoCapture(0)
if not cap.isOpened():
    raise IOError("❌ No se pudo abrir la cámara.")

device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Usando dispositivo: {device}")

model = YOLO('yolo11x.pt')
model.to(device)

BALL_CLASS_ID = 32

# Posición por defecto (centro del campo)
position = "0.5000,0.5000,0.0000"

while True:
    success, img = cap.read()
    if not success or img is None:
        continue

    img_resized = cv2.resize(img, (640, 360))
    results = model(img_resized, device=device, stream=False, verbose=False)

    for r in results:
        for box in r.boxes:
            if int(box.cls[0]) == BALL_CLASS_ID:
                x1, y1, x2, y2 = map(int, box.xyxy[0])

                # Ajustar a tamaño original
                scale_x = img.shape[1] / img_resized.shape[1]
                scale_y = img.shape[0] / img_resized.shape[0]
                x1, y1, x2, y2 = int(x1*scale_x), int(y1*scale_y), int(x2*scale_x), int(y2*scale_y)

                cx, cy = x1 + (x2-x1)//2, y1 + (y2-y1)//2

                cvzone.cornerRect(img, (x1, y1, x2-x1, y2-y1), colorR=(0,255,255))
                cvzone.putTextRect(img, "Ball", (x1, y1-10), colorR=(0,255,255), scale=1, thickness=1)

                # NORMALIZAR A 0..1 Y CAMBIAR LOS EJES PARA QUE NO ESTEN INVERTIDOS
                cx_norm = cy / img.shape[0]      # 0 (arriba) → 1 (abajo)
                cy_norm = cx / img.shape[1]      # 0 (izq) → 1 (derecha)

                # Invertimos Y para que 0 sea abajo y 1 arriba (como en Unity)
                cy_norm = 1.0 - cy_norm

                # Guardamos como "x,y,z" (z=0 de momento)
                position = f"{cx_norm:.4f},{cy_norm:.4f},0.0000"

    cv2.imshow("Ball Detection", img)

    # Guardar la última posición detectada
    try:
        with open(OUTPUT_PATH, "w") as f:
            f.write(position + "\n")
    except PermissionError:
        # Unity está leyendo justo ahora; ignoramos este frame
        pass

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
