import time
import numpy as np
import pandas as pd
import cv2
import json
from pupil_labs.realtime_api.simple import discover_one_device


class PupilClient:
    def __init__(self):
        self.device = None
        self.frame = None
        self.gaze = None
        self.timestamp = None

    def connect(self):
        while self.device is None:
            print("Searching for Pupil Labs device...\n")
            device = discover_one_device(max_search_duration_seconds=10.0)

            if device:
                self.device = device
                print(f"Connected to Pupil Labs device: {self.device}")
            else:
                timeout = 5
                print(f"No devices found. Retrying in {timeout} seconds...\n")
                time.sleep(timeout)

        print("Connection established with Pupil Labs device.")

    def get_calibration(self):
        calibration = self.device.get_calibration()

        matrix = calibration["scene_camera_matrix"]
        dist = calibration["scene_distortion_coefficients"]
        
        if matrix.shape != (3, 3):
            matrix = matrix.reshape((3, 3))

        return matrix, dist[:5]
        
    def stream(self):
        """
        Streams matched scene video + gaze data
        """ 
        if self.device is None:
            raise RuntimeError("Not connected to any Pupil Labs device.")
           
        print("Starting data stream...")

        try:
            while True:
                frame, gaze = self.device.receive_matched_scene_video_frame_and_gaze()

                self.frame = frame.bgr_pixels
                self.timestamp = frame.timestamp_unix_seconds

                self.gaze = (gaze.x, gaze.y)
  
                yield {
                    "frame": self.frame,
                    "gaze": self.gaze,
                    "timestamp": self.timestamp
                }
        except Exception as e:
            print(f"Stream error: {e}")
        finally:
            if self.device:
                self.device.close()


class ReplayPupilClient:
    def __init__(self, video_path, gaze_path):
        self.cap = cv2.VideoCapture(video_path)
        self.gaze = pd.read_csv(gaze_path)

        first_ts = self.gaze["timestamp [ns]"].iloc[0]
        self.gaze["ts_sec"] = (self.gaze["timestamp [ns]"] - first_ts) / 1e9

    def get_data(self):
        ret, frame = self.cap.read()
        if not ret:
            return None
        
        timestamp_video = self.cap.get(cv2.CAP_PROP_POS_MSEC) / 1000.0

        gaze_row = self.__get__closest(self.gaze, timestamp_video)

        gaze_data = None
        if gaze_row is not None:
            is_valid = (gaze_row['worn'] == 1) and pd.isna(gaze_row['blink id'])

            if is_valid:
                gaze_data = (gaze_row['gaze x [px]'], gaze_row['gaze y [px]'])

        return {
            "frame": frame,
            "timestamp": timestamp_video,
            "gaze": gaze_data,
        }
    
    def get_calibration(self):
        with open(r"C:\Users\talos\Downloads\02_Belisarius Brown\02_Belisarius Brown\2024-03-13_19-43-27-92efb037\scene_camera.json") as f:
            calib = json.load(f)

        camera_matrix = np.array(calib["camera_matrix"])
        dist_coeffs = np.array(calib["dist_coefs"]).flatten()

        return camera_matrix, dist_coeffs

    def __get__closest(self, df, timestamp):
        if df is None or df.empty:
            return None
        
        idx = (df["ts_sec"] - timestamp).abs().idxmin()
        return df.iloc[idx]

    def stream(self):
        while True:
            data = self.get_data()
            if data is None:
                break
            yield data