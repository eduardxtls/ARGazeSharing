import numpy as np
import cv2 as cv


class ObjectDetector:
    def __init__(self, model):
        self.model = model

    def detect_objects(self, frame):
        """
        Detects objects in the given frame
        Returns: list of detected objects with their bounding boxes
        """
        
        results = self.model(frame, verbose=False)[0]
        detections = []

        for result in results:
            boxes = result.boxes
            masks = result.masks

            for i, box in enumerate(boxes):
                cls = int(box.cls[0])
                label = self.model.names[cls]
                conf = float(box.conf[0])

                x1, y1, x2, y2 = map(int, box.xyxy[0])

                mask_poly = None
                if masks is not None:
                    mask_poly = masks.xy[i].astype(np.int32)

                detections.append({
                    "label": label,
                    "confidence": conf,
                    "bbox": [x1, y1, x2, y2],
                    "mask": mask_poly
                })

        return detections
    
    def get_box_color(self, frame, detection):
        """
        Assigns a color to the detected object's 50% bounding box
        """
        
        x1, y1, x2, y2 = map(int, detection["bbox"])
        w, h = x2 - x1, y2 - y1
        cx, cy = x1 + w // 2, y1 + h // 2

        nx1 = max(x1, cx - w // 4)
        ny1 = max(y1, cy - h // 4)
        nx2 = min(x2, cx + w // 4)
        ny2 = min(y2, cy + h // 4)

        roi = frame[ny1:ny2, nx1:nx2]
        
        if roi.size == 0:
            return (0, 0, 0)
        
        mask = detection.get("mask")
        
        if mask is not None:
            roi_mask = np.zeros(frame.shape[:2], dtype=np.uint8)
            cv.fillPoly(roi_mask, [mask], 255)
            roi_mask_cropped = roi_mask[y1:y2, x1:x2]

            mean_color = cv.mean(roi, mask=roi_mask_cropped)[:3]
        else:
            mean_color = cv.mean(roi)[:3]

        return mean_color
    
    def get_gaze_color(self, frame, gaze, radius=10):
        if gaze is None:
            return (0, 0, 0)
        
        x, y = int(gaze[0]), int(gaze[1])
        h, w, _ = frame.shape

        x1, y1 = max(0, x - radius), max(0, y - radius)
        x2, y2 = min(w, x + radius), min(h, y + radius)

        gaze_roi = frame[y1:y2, x1:x2]

        if gaze_roi.size == 0:
            return (0, 0, 0)

        return cv.mean(gaze_roi)[:3]
    
    def get_color_name_hsv(self, bgr_color):
        """
        Converts a color into a readable name
        """

        color_pixel = np.uint8([[bgr_color]])

        # Pixel is not in BGR format
        if color_pixel.shape[2] != 3:
            return "Error"

        # HSV values and threshold computations to define colors
        hsv_pixel = cv.cvtColor(color_pixel, cv.COLOR_BGR2HSV)[0][0]
        h, s, v = hsv_pixel

        if s < 20:
            if v < 50:  return "Black"
            if v > 200: return "White"
            return "Gray"
        
        if v < 40: return "Black"

        if h < 10 or h >= 160:  return "Red"
        if 10 <= h < 25:        return "Orange"
        if 25 <= h < 35:        return "Yellow"
        if 35 <= h < 85:        return "Green"
        if 85 <= h < 130:       return "Blue"
        if 130 <= h < 160:      return "Purple"

        return "Gray" # Fallback for undefined colors