import cv2
from datetime import datetime
from ultralytics import YOLO

# Internal imports
from gaze.pupil_client import PupilClient
from gaze.pupil_client import ReplayPupilClient
from vision.detector import ObjectDetector
from fusion.gaze_object_fusion import GazeObjectFusion
from spatial.aruco_tracker import ArucoTracker
from networking.udp_sender import UDPSender

MODE = "live"  # Options: "live", "replay"

def main():
    # Vision model selection
    model = YOLO("models/yolo11s_ingredients.pt")

    if MODE == "live":
        client = PupilClient()
        client.connect()
    else:
        video_path = r""
        gaze_csv_path = r""

        client = ReplayPupilClient(video_path, gaze_csv_path)

    # Fetch camera intrinsics
    camera_matrix, dist_coeffs = client.get_calibration()

    detector = ObjectDetector(model)
    fusion = GazeObjectFusion()

    # Establish connection to Pupil Labs eye tracker and start Aruco tracker
    if MODE == "live":
        # tracker = ArucoTracker(camera_matrix, dist_coeffs=dist_coeffs)
        tracker = None # Leave both cases without tracker for now
    else:
        tracker = None  # No tracking in replay mode

    # Create UDP bridge
    udp_bridge = UDPSender(ip="127.0.0.1", port=8080)

    # Recording
    is_recording = False
    save_local_video = True
    video_writer = None

    try:
        for data in client.stream():
            frame = data["frame"]
            gaze = data["gaze"]
            h, w, _ = frame.shape

            if save_local_video and video_writer is None:
                filename = f"PupilLabs_Recording_{datetime.now().strftime('%Y%m%d_%H%M%S')}.mp4"
                fourcc = cv2.VideoWriter_fourcc(*'mp4v')
                video_writer = cv2.VideoWriter(filename, fourcc, 10.0, (w, h))
                print(f"Started local video recording: {filename}")

            # Map gaze to pixel coordinates
            gaze_point = None
            if gaze is not None:
                gaze_point = gaze

            # Detect objects in the frame
            detections = detector.detect_objects(frame)

            # Estimate pose of the camera relative to marker
            pose = tracker.estimate_pose(frame) if tracker is not None else None

            # Find looked-at/selected object
            selected_object = None
            if gaze_point:
                selected_object = fusion.find_looked_at_object(gaze_point, detections)

            if pose and selected_object:
                R_vec, tvec = pose

                x1, y1, x2, y2 = selected_object["bbox"]
                object_center = ((x1 + x2) / 2, (y1 + y2) / 2)

                relative_position = ArucoTracker.estimate_object_position(object_center, camera_matrix, R_vec, tvec)
                
                udp_bridge.send_spatial_data(
                    label=selected_object["label"],
                    rel_x=float(relative_position[0]),
                    rel_y=float(relative_position[1]),
                    rel_z=float(relative_position[2])
                )

            # Draw detections
            for detection in detections:
                x1, y1, x2, y2 = map(int, detection["bbox"])
                label = detection["label"]
                mask = detection.get("mask")

                is_focused = (selected_object is not None and detection == selected_object)

                if is_focused:
                    color = (0, 255, 0)  # Green for focused object
                    thickness = 2

                    if mask is not None:
                        mask_overlay = frame.copy()
                        cv2.fillPoly(mask_overlay, [mask], color)
                        cv2.addWeighted(mask_overlay, 0.3, frame, 0.7, 0, frame)
                        cv2.polylines(frame, [mask], True, color, thickness)
                    else:
                        cv2.rectangle(frame, (x1, y1), (x2, y2), color, thickness)

                    cv2.putText(frame, f"Looking at: {label}", (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)
                else:
                    color = (200, 0, 0)  # Blue for non-focused objects
                    cv2.rectangle(frame, (x1, y1), (x2, y2), color, 1)
                    cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 1)

            # Visualization of Gaze Point
            if gaze_point:
                center = (int(gaze_point[0]), int(gaze_point[1]))
                cv2.circle(frame, center, 10, (0, 255, 0), -1)

            # Recording
            if MODE == "live":
                recording_text = "Recording ..." if is_recording else "Press R to start recording."
                recording_color = (0, 0, 255) if is_recording else (255, 255, 255)
                cv2.putText(frame, recording_text, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, recording_color, 2)

            if video_writer is not None:
                video_writer.write(frame)

            cv2.imshow("Gaze + Object Detection", frame)

            # Start and stop recording
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

            # Quit with Q key
            if cv2.waitKey(1) & 0xFF == ord('q'):
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