import socket

UNITY_IP = "127.0.0.1"
UNITY_PORT = 5005

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def enviar_resultado(resultado):
    sock.sendto(resultado.encode(), (UNITY_IP, UNITY_PORT))

# Simulación de detección LiDAR
gol_detectado = True
fallo_detectado = False

if gol_detectado:
    enviar_resultado("GOL")

if fallo_detectado:
    enviar_resultado("FALLO")
