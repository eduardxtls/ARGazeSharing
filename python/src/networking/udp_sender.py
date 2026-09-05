import socket
import json


class UDPSender:
    def __init__(self, ip="192.168.1.XX", port=5005):
        self.ip = ip
        self.port = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    def send_spatial_data(self, label, color, position, tvec, rvec, gaze_point):
        try:
            # Most data unused in current version
            data = {
                "type": "detection",
                "label": str(label) if label else "None",
                "color": str(color) if color else "Unknown",
                "position": position if position is not None else [0.0, 0.0, 0.0],
                "marker_tvec": tvec.flatten().tolist() if tvec is not None else [0, 0, 0],
                "marker_rvec": rvec.flatten().tolist() if rvec is not None else [0, 0, 0],
                "gaze_2d": list(gaze_point) if gaze_point else [0, 0]
            }

            message = json.dumps(data)
            self.sock.sendto(message.encode(), (self.ip, self.port))

        except Exception as e:
            print(f"UDP send error: {e}")