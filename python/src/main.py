import cv2
import time
from datetime import datetime
from ultralytics import YOLO
from pupil_labs.realtime_api import FixationOnsetEventData, FixationEventData

# Internal imports
from gaze.pupil_client import PupilClient
from gaze.pupil_client import ReplayPupilClient
from vision.detector import ObjectDetector
from fusion.gaze_object_fusion import GazeObjectFusion
from spatial.aruco_tracker import ArucoTracker
from networking.udp_sender import UDPSender

# Mode selection: "live" for real-time streaming or "replay" for recordings
MODE = "live"

def main():
    # Vision model selection
    model = YOLO(".\python\models\yolo26n.pt")

    # Object recognition parameters
    AI_UPDATE_INTERVAL = 0.2
    last_ai_update_time = 0

    if MODE == "live":
        client = PupilClient()
        client.connect()
        # Aruco parameters
        tracker = ArucoTracker(client.get_calibration(), marker_length=0.1)
        rvec, tvec = None, None
        ARUCO_UPDATE_INTERVAL = 0.25
        last_aruco_update_time = 0
        udp_bridge = UDPSender(ip="127.0.0.1", port=8080)
    else:
        video_path = r"C:\Users\talos\Downloads\02_Belisarius Brown\02_Belisarius Brown\2024-03-13_19-43-27-92efb037\5ab15f0b_0.0-389.913.mp4"
        gaze_csv_path = r"C:\Users\talos\Downloads\02_Belisarius Brown\02_Belisarius Brown\2024-03-13_19-43-27-92efb037\gaze.csv"

        client = ReplayPupilClient(video_path, gaze_csv_path)
        tracker = None
        udp_bridge = None

    # Instantiate core components
    detector = ObjectDetector(model)
    fusion = GazeObjectFusion()

    # Recording
    is_recording = False
    save_local_video = False
    video_writer = None
    target_fps = 30.0
    frame_interval = 1.0 / target_fps
    last_write_time = time.time()
    last_frame = None

    # Fixations
    is_fixating = False
    detections = []
    selected_object = None

    try:
        for data in client.stream():
            frame = data["frame"]
            gaze = data["gaze"]
            current_time = time.time()

            # Initialize local video
            if save_local_video and video_writer is None:
                h, w, _ = frame.shape
                filename = f"PupilLabs_Recording_{datetime.now().strftime('%Y%m%d_%H%M%S')}.mp4"
                fourcc = cv2.VideoWriter_fourcc(*'mp4v')
                video_writer = cv2.VideoWriter(filename, fourcc, target_fps, (w, h))
                print(f"Started local video recording: {filename}")

            # Tracking marker
            if current_time - last_aruco_update_time > ARUCO_UPDATE_INTERVAL and tracker is not None:
                pose = tracker.estimate_pose(frame)
                if pose is not None:
                    rvec, tvec = pose
                    last_aruco_update_time = current_time
                else:
                    pass

            if MODE == "live":
                event = client.device.receive_eye_events(0.0)
                if isinstance(event, FixationOnsetEventData):
                    is_fixating = True
                elif isinstance(event, FixationEventData):
                    is_fixating = False
                    selected_object = None
            else:
                is_fixating = gaze is not None # Always fixate in replay mode

            # Reduce interval for object recognition
            if is_fixating and (current_time - last_ai_update_time) > AI_UPDATE_INTERVAL:
                detections = detector.detect_objects(frame)
                selected_object = fusion.find_looked_at_object(gaze, detections)
                last_ai_update_time = current_time

            # Draw detections
            for detection in detections:
                x1, y1, x2, y2 = detection["bbox"]
                label = detection["label"]
                mask = detection.get("mask")
                is_focused = (selected_object is not None and detection == selected_object)

                if is_focused:
                    color = (0, 200, 0)  # Green for focused
                    rel_pos_cm = [0.0, 0.0, 0.0]

                    if mask is not None:
                        # Mask as overlay
                        mask_overlay = frame.copy()
                        cv2.fillPoly(mask_overlay, [mask], color)
                        cv2.addWeighted(mask_overlay, 0.3, frame, 0.7, 0, frame)
                        cv2.polylines(frame, [mask], True, color, 2)
                    else:
                        # Bounding box
                        cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
                    cv2.putText(frame, f"Looking at: {label}", (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

                    if rvec is not None and tvec is not None and gaze is not None:
                        _, gazepos = tracker.estimate_object_and_gaze_position(rvec, tvec, gaze)

                        if gazepos is not None:
                            gx, gy, gz = gazepos.flatten()
                            
                            rel_pos_cm = [
                                int(gx * 1000) / 10.0,
                                int(gy * 1000) / 10.0,
                                int(gz * 1000) / 10.0
                            ]

                            cv2.putText(frame, f"Position of gaze: {rel_pos_cm}", (x1, y1 - 35), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)


                    # Color of object
                    # bgr_object = detector.get_box_color(frame, detection)
                    # color = detector.get_color_name_hsv(bgr_object)

                    # Color around gaze point
                    gaze_color = detector.get_gaze_color(frame, gaze)
                    color = detector.get_color_name_hsv(gaze_color)

                    # Send UDP data
                    if udp_bridge is not None:
                        udp_bridge.send_spatial_data(
                            label=label,
                            color=color,
                            position=rel_pos_cm,
                            tvec=tvec,
                            rvec=rvec,
                            gaze_point=gaze
                        )

                else:
                    color = (100, 0, 0)  # Blue for non-focused
                    cv2.rectangle(frame, (x1, y1), (x2, y2), color, 1)
                    cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 1)

            # Visualization of gaze point
            if gaze:
                center = (int(gaze[0]), int(gaze[1]))
                cv2.circle(frame, center, 5, (0, 255, 0), -1)

            # Aruco axes
            if rvec is not None:
                tracker.draw_marker_axes(frame, rvec, tvec)

            # Video saving workaround for frame (not regular sometimes)
            if video_writer is not None:
                last_frame = frame
                now = time.time()

                while now - last_write_time >= frame_interval:
                    if last_frame is not None:
                        video_writer.write(last_frame)

                    last_write_time += frame_interval

            # Recording text on display
            if MODE == "live":
                recording_text = "Recording ..." if is_recording else "Press R to start recording."
                recording_color = (0, 0, 255) if is_recording else (255, 255, 255)
                cv2.putText(frame, recording_text, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, recording_color, 2)

            cv2.imshow("Gaze + Object Detection", frame)

            # Input: Start/stop recording and quit
            key = cv2.waitKey(1) & 0xFF

            if key == ord('r') and MODE == "live":
                if not is_recording:
                    client.device.recording_start()
                    is_recording = True
                    print("Started recording.")
                else:
                    client.device.recording_stop_and_save()
                    is_recording = False
                    print("Stopped recording and saved data.")

            elif key == ord('q'):
                break
        
    except KeyboardInterrupt:
        print("Stopping...")
    finally:
        if video_writer is not None:
            video_writer.release()
            print("Local video saved.")
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()