import torch

print("Versión de PyTorch:", torch.__version__)
print("CUDA disponible:", torch.cuda.is_available())
if torch.cuda.is_available():
    print("Número de GPUs:", torch.cuda.device_count())
    print("Nombre de la GPU:", torch.cuda.get_device_name(0))


# pip uninstall torch torchvision torchaudio -y


# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118 
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
# pip install --pre torch torchvision torchaudio --index-url https://download.pytorch.org/whl/nightly/cu128
 
