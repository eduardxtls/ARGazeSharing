from ultralytics import YOLO

# Load model
model = YOLO(r"D:\Masterarbeit\TIK\Seeing-Through-Others-Eyes\python\models\yolo11s_ingredients_augmented.pt")

# Export to ONNX for Sentis
model.export(format="onnx")