import socket
import json

UDP_IP = "10.175.57.181" 
# UDP_IP = "127.0.0.1"
UDP_PORT = 8080

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

print(f"Sending label-only JSON to {UDP_IP}:{UDP_PORT}")
print("Type 'exit' to quit.\n")

while True:
    label = input("Label: ")

    if label.lower() == "exit":
        break

    message = {
        "type": "detection",
        "label": label
    }

    json_msg = json.dumps(message)
    sock.sendto(json_msg.encode("utf-8"), (UDP_IP, UDP_PORT))

    print("Sent:", json_msg)

print("Sender closed.")