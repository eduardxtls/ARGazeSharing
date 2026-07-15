from ultralytics import YOLO

# Load your trained or pre-trained YOLO model
model = YOLO(r"D:\Masterarbeit\TIK\Seeing-Through-Others-Eyes\python\models\yolo11s_ingredients.pt")

# Export to ONNX for Sentis
model.export(format="onnx")