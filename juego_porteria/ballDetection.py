import cv2              # OpenCV, para capturar video y procesar imágenes
import numpy as np      # Librería para operaciones matemáticas y matrices
from ultralytics import YOLO  # Modelo YOLO para detección de objetos
import cvzone           # Para dibujar rectángulos y texto fácilmente
import torch            # PyTorch, para usar GPU si está disponible

# Abrir la cámara por defecto (0)
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    raise IOError("❌ No se pudo abrir la cámara.")

# Detectar si hay GPU disponible; usar GPU si es posible
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Usando dispositivo: {device}")

# Cargar el modelo YOLO y moverlo al dispositivo adecuado (GPU o CPU)
model = YOLO('yolo11x.pt')
model.to(device)

# Definir la clase de objeto que queremos detectar (pelota)
BALL_CLASS_ID = 32

# Str donde se guardarán las posiciones de la pelota en cada frame
position = "0.0000,0.0000,0.0000"

# Bucle principal para procesar video en tiempo real
while True:
    # Leer un frame de la cámara
    success, img = cap.read()
    if not success or img is None:
        continue  # Si falla la captura, saltar al siguiente frame

    # Redimensionar el frame para acelerar la detección
    img_resized = cv2.resize(img, (640, 360))

    # Pasar el frame redimensionado por el modelo YOLO
    results = model(img_resized, device=device, stream=False, verbose=False)

    # Recorrer todas las detecciones del modelo
    for r in results:
        for box in r.boxes:
            # Filtrar solo la clase "pelota"
            if int(box.cls[0]) == BALL_CLASS_ID:
                # Obtener coordenadas del bounding box (esquinas)
                x1, y1, x2, y2 = map(int, box.xyxy[0])

                # Ajustar coordenadas al tamaño original del frame
                scale_x = img.shape[1] / img_resized.shape[1]
                scale_y = img.shape[0] / img_resized.shape[0]
                x1, y1, x2, y2 = int(x1*scale_x), int(y1*scale_y), int(x2*scale_x), int(y2*scale_y)

                # Calcular el centro de la pelota
                cx, cy = x1 + (x2-x1)//2, y1 + (y2-y1)//2

                # Dibujar un rectángulo con esquinas resaltadas alrededor de la pelota
                cvzone.cornerRect(img, (x1, y1, x2-x1, y2-y1), colorR=(0,255,255))

                # Añadir texto "Ball" sobre el rectángulo
                cvzone.putTextRect(img, "Ball", (x1, y1-10), colorR=(0,255,255), scale=1, thickness=1)

                # Guardar la posición del centro de la pelota en la lista
                # Coordenadas normalizadas y eje Y invertido (para animación)
                position = f"{cx/100:.4f},{(img.shape[0]-cy)/100:.4f},0.0000"

    # Mostrar el frame con detecciones en pantalla
    cv2.imshow("Ball Detection", img)
    
    # Guardar todas las posiciones de las pelotas en un archivo de texto
    with open("AnimationFile.txt", "w") as f:
        f.write(f"{position}\n")
        
    # Salir si se presiona la tecla 'q'    
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Liberar la cámara y cerrar ventanas
cap.release()
cv2.destroyAllWindows()
