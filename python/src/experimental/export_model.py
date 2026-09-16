from ultralytics import YOLO

# Load model
model = YOLO("models/yolo11s_ingredients.pt")

# Export to ONNX for Sentis
model.export(format="onnx")