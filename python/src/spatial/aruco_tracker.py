import cv2
import numpy as np


class ArucoTracker:
    def __init__(self, calibration, marker_length=0.1):
        self.camera_matrix, self.dist_coeffs = calibration
        self.marker_length = marker_length

        self.dictionary = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50)
        self.parameters = cv2.aruco.DetectorParameters()
        self.detector = cv2.aruco.ArucoDetector(self.dictionary, self.parameters)

        self.obj_points = np.array([
            [-marker_length / 2, marker_length / 2,  0],
            [marker_length / 2,  marker_length / 2,  0],
            [marker_length / 2,  -marker_length / 2, 0],
            [-marker_length / 2, -marker_length / 2, 0],
        ], dtype=np.float32)

    def estimate_pose(self, frame):
        corners, ids, _ = self.detector.detectMarkers(frame)
        
        if ids is not None and len(ids) > 0:
            success, rvec, tvec = cv2.solvePnP(
                self.obj_points,
                corners[0],
                self.camera_matrix,
                self.dist_coeffs
            )

            if success:
                return rvec, tvec
        
        return None
    
    def estimate_object_and_gaze_position(self, rvec, tvec, gaze_2d):
        """
        Estimates the 3D position of an object in the marker's coordinate system given its pixel coordinates in the image.
        """
        # Rotation matrix
        R, _ = cv2.Rodrigues(rvec)
        R_inv = R.T
        camera_pos_marker_space = -R_inv @ tvec

        # Convert 2D point to 3D ray
        K_inv = np.linalg.inv(self.camera_matrix)
        pixel_homog = np.array([gaze_2d[0], gaze_2d[1], 1.0]).reshape(3, 1)
        ray_camera_space = K_inv @ pixel_homog

        # Rotate ray into marker space
        ray_marker_space = R_inv @ ray_camera_space

        # Find intersection
        cam_z = camera_pos_marker_space[2, 0]
        ray_z = ray_marker_space[2, 0]

        if abs(ray_z) < 1e-6:
            return camera_pos_marker_space, None
        
        s = -cam_z / ray_z
        gaze_3d_marker_space = camera_pos_marker_space + s * ray_marker_space

        return camera_pos_marker_space, gaze_3d_marker_space

    
    def draw_marker_axes(self, frame, rvec, tvec):
        if rvec is not None and tvec is not None:
            cv2.drawFrameAxes(
                frame,
                self.camera_matrix,
                self.dist_coeffs,
                rvec, tvec,
                self.marker_length * 0.5
            )