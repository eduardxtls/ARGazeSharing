import math


class GazeObjectFusion:
    def find_looked_at_object(self, gaze_point, detections):
        """
        Returns the object closest to the gaze point
        """
        if gaze_point is None:
            return None

        # Fetch gaze coordinates
        gaze_x, gaze_y = gaze_point

        # Find objects within which the gaze point is located
        containing_objects = []
        for detection in detections:
            x1, y1, x2, y2 = detection["bbox"]

            if x1 <= gaze_x <= x2 and y1 <= gaze_y <= y2:
                area = (x2 - x1) * (y2 - y1)
                containing_objects.append((area, detection))

        # Sort by area and return smallest object
        if containing_objects:
            containing_objects.sort(key=lambda x: x[0])
            return containing_objects[0][1]
        
        # If gaze slightly off, find closest object
        closest_object = None
        min_distance = float('inf')

        for detection in detections:
            x1, y1, x2, y2 = detection["bbox"]

            closest_x = max(x1, min(gaze_x, x2))
            closest_y = max(y1, min(gaze_y, y2))

            dist = math.sqrt((gaze_x - closest_x) ** 2 + (gaze_y - closest_y) ** 2) # Euclidean

            if dist < min_distance:
                min_distance = dist
                closest_object = detection

        # Older code
        """
        # Initiate search parameters
        closest_object = None
        min_distance = float('inf')

        # Iterate through detections to find the closest one to the gaze point
        for detection in detections:
            x1, y1, x2, y2 = detection["bbox"]

            center_x = (x1 + x2) // 2
            center_y = (y1 + y2) // 2

            # Euclidean distance between gaze point and object center
            distance = math.sqrt((gaze_x - center_x) ** 2 + (gaze_y - center_y) ** 2)

            if distance < min_distance:
                min_distance = distance
                closest_object = detection

        if min_distance > 50:  # Threshold to filter out objects that are too far
            return None
        """

        # Check for threshold (50 pixels)
        return closest_object if min_distance < 50 else None