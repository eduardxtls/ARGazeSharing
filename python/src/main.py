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

# Modes selection:
# "live" for real-time streaming or "replay" for recordings
MODE = "live"

# "selective" for dwell filter and "continuous" for constant feedback
# In selective mode, feedback is provided only after a given fixation threshold
FEEDBACK = "selective" 
DWELL_THRESHOLD = 3.0

# Aruco tracking toggle
ARUCO = False


def main():
    # Vision model selection
    model = YOLO(".\python\models\yolo12n.pt")

    # Object recognition parameters
    AI_UPDATE_INTERVAL = 0.2
    last_ai_update_time = 0

    if MODE == "live":
        client = PupilClient()
        client.connect()
        udp_bridge = UDPSender(ip="127.0.0.1", port=8080)

        # Aruco parameters
        if ARUCO:
            tracker = ArucoTracker(client.get_calibration(), marker_length=0.1)
            rvec, tvec = None, None
            ARUCO_UPDATE_INTERVAL = 0.25
            last_aruco_update_time = 0
        else:
            tracker = None

    else:
        video_path = r"" # Local path to video
        gaze_csv_path = r""
        client = ReplayPupilClient(video_path, gaze_csv_path)
        udp_bridge = None
        tracker = None

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

    # Fixations and current detections
    is_fixating = False
    detections = []
    selected_object = None

    # Dwell filter variables
    current_focused_label = None
    dwell_start_time = None
    dwell_triggered = False

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

            # Tracking the marker
            if ARUCO and tracker is not None:
                if current_time - last_aruco_update_time > ARUCO_UPDATE_INTERVAL:
                    pose = tracker.estimate_pose(frame)
                    # Update only if pose estimation was successful (no occlusion etc.)
                    if pose is not None:
                        rvec, tvec = pose
                        last_aruco_update_time = current_time

            # PL Invisible
            '''
            if MODE == "live":
                is_fixating = True
            '''

            # PL Neon
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

            # Dwell filter
            if selected_object is not None:
                new_label = selected_object["label"]

                if new_label != current_focused_label:
                    current_focused_label = new_label
                    dwell_start_time = current_time
                    dwell_triggered = False
                else:
                    elapsed_dwell_time = current_time - dwell_start_time
                    if elapsed_dwell_time >= DWELL_THRESHOLD:
                        print(f"Dwell triggered on {current_focused_label} after {elapsed_dwell_time:.1f} seconds.")
                        dwell_triggered = True
            else:
                # No object selected --> reset
                current_focused_label = None
                dwell_start_time = None
                dwell_triggered = False

            # Draw detections
            for detection in detections:
                x1, y1, x2, y2 = detection["bbox"]
                label = detection["label"]

                # No person detection
                if label == "person":
                    break
                
                # If segmentation model is used, mask is available -- otherwise returns None
                mask = detection.get("mask")

                is_looking_at_this = (selected_object is not None and detection == selected_object)

                # Set focus based on feedback mode
                if FEEDBACK == "selective":
                    is_focused = is_looking_at_this and dwell_triggered
                else:
                    is_focused = is_looking_at_this

                if is_focused:
                    color = (0, 200, 0)  # Green for focused
                    rel_pos_cm = [0.0, 0.0, 0.0]

                    # For segmentation models, mask is available
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

                    # Estimation of gaze coordinates in code space
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

                    # Color within bounding box -- inaccurate in most cases
                    # bgr_object = detector.get_box_color(frame, detection)
                    # color_name = detector.get_color_name_hsv(bgr_object)

                    # Color around gaze point
                    gaze_color = detector.get_gaze_color(frame, gaze)
                    color_name = detector.get_color_name_hsv(gaze_color)

                    # Send UDP data
                    if udp_bridge is not None:
                        udp_bridge.send_spatial_data(
                            label=label,
                            color=color_name,
                            position=rel_pos_cm,
                            tvec=tvec,
                            rvec=rvec,
                            gaze_point=gaze
                        )

                else:
                    color = (100, 0, 0)  # Blue for non-focused
                    cv2.rectangle(frame, (x1, y1), (x2, y2), color, 1)
                    cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 1)

                    # Dwell progress bar for "selective" feedback
                    if FEEDBACK == "selective":
                        progress = min((current_time - dwell_start_time) / DWELL_THRESHOLD, 1.0)
                        cv2.rectangle(frame, (x1, y2 + 5), (x1 + int((x2 - x1) * progress), y2), (0, 165, 255), -1)

            # Visualization of gaze point
            if gaze:
                center = (int(gaze[0]), int(gaze[1]))
                cv2.circle(frame, center, 5, (0, 255, 0), -1)

            # Aruco axes
            if tracker is not None and rvec is not None:
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