from datetime import datetime
import socket
import threading

participant_id = input("Enter Participant ID (e.g. P01): ").strip()
timestamp_str = datetime.now().strftime("%Y%m%d_%H%M%S")
csv_path = f"experiment_log_{participant_id}_{timestamp_str}.csv"

# Default to test condition
current_condition = "Test"

with open(csv_path, "w") as f:
    f.write("participant_id,condition_code,timestamp,target_item,total_time_sec,mistakes_count\n")

print(f"Logging session for {participant_id}")
print(f"Initial condition set to: {current_condition}")
print(f"Type a new condition and press ENTER to switch.")

# Thread function to capture condition changes from terminal
def condition_listener():
    global current_condition
    while True:
        user_input = input().strip()
        if user_input:
            current_condition = user_input.upper()
            print(f"\nSwitched active condition to: {current_condition}\n")

input_thread = threading.Thread(target=condition_listener, daemon=True)
input_thread.start()

# TCP setup
local_ip = "0.0.0.0"
log_port = 5005
server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server.bind((local_ip, log_port))
server.listen(5)

try:
    while True:
        client_socket, addr = server.accept()
        try:
            data = client_socket.recv(1024)
            if data:
                raw_payload = data.decode("utf-8").strip()
                log_line = f"{participant_id},{current_condition},{raw_payload}\n"
                
                with open(csv_path, "a") as f:
                    f.write(log_line)
                    
                print(f"[{current_condition}] Logged: {raw_payload}")
        except Exception as e:
            print(f"Error receiving data: {e}")
        finally:
            client_socket.close()
except KeyboardInterrupt:
    print(f"\nStopped logging session. File is saved as: {csv_path}")
    server.close()