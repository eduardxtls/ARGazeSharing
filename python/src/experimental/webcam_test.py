import cv2
from ultralytics import YOLO

model = YOLO(r"D:\Masterarbeit\TIK\Seeing-Through-Others-Eyes\python\models\yolo11s_ingredients.onnx")

# Connect to webcam
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Error: Couldn't open webcam.")
    exit()

print("Webcam stream started. Press 'Q' to quit.")

while True:
    ret, frame = cap.read()

    # Run YOLO inference on current frame
    results = model(frame, conf=0.75, verbose=False, stream=True)

    for result in results:
        # Use YOLO's built-in visualizer to render masks & boxes
        annotated_frame = result.plot()

    # Display the live annotated video
    cv2.imshow("YOLO Custom Model Test", annotated_frame)

    # Check for quit key
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()