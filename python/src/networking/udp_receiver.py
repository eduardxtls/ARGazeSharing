import socket
import json
import time

class UDPReceiver:
    def __init__(self, ip="0.0.0.0", port=5005):
        self.ip = ip
        self.port = port

        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        self.sock.bind((self.ip, self.port))

        print(f"UDP Receiver initialized on {self.ip}:{self.port}")

    def receive_data(self):
        try:
            data, addr = self.sock.recvfrom(4096)  # buffer size is 1024 bytes
            decoded_message = json.loads(data.decode('utf-8'))

            return decoded_message
        
        except socket.timeout:
            return None, None
        
        except Exception as e:
            print(f"Error receiving data: {e}")
            return None, None
        
    def close(self):
        self.sock.close()

if __name__ == "__main__":
    receiver = UDPReceiver(port=8080)
    
    last_print_time = 0
    UPDATE_RATE = 1.0
            
    try:
        while True:
            data = receiver.receive_data()
            current_time = time.time()
            
            if data and (current_time - last_print_time) >= UPDATE_RATE:
                if data.get("type") == "detection":
                    label = data.get("label")
                    color = data.get("color")
                    position = data.get("position")
                    marker_tvec = data.get("marker_tvec")
                    marker_rvec = data.get("marker_rvec")
                    gaze_2d = data.get("gaze_2d")

                    print(f"Object: {label}, Color: {color}, Position: {position}", "Marker Rotation: ", marker_rvec, "Marker Translation: ", marker_tvec, "Gaze 2D: ", gaze_2d)
                elif data.get("type") == "design":
                    param = data.get("param")
                    value = data.get("value")
                    print(f"Design Update - {param}: {value}")
                else:
                    print(f"Received unknown data type: {data.get('type')}")
                
                last_print_time = current_time

    except KeyboardInterrupt:
        print("\nStopping UDP Receiver...")
    finally:
        receiver.close()