import socket
import json
from nicegui import ui

# --- UDP Configuration ---
UNITY_IP = "127.0.0.1" 
UNITY_PORT = 8080
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

def send_update(param_type, value):
    """Sends a JSON string via UDP to Unity."""
    data = {
        "type": "design",
        "parameter": param_type,
        "value": value
    }
    message = json.dumps(data)
    sock.sendto(message.encode(), (UNITY_IP, UNITY_PORT))
    ui.notify(f"{param_type.capitalize()}: {value}", color='slate-700', pos='bottom-right')

# --- Data Definitions ---
SIZES = ['Tiny', 'Small', 'Medium', 'Large', 'Huge']
CUE_TYPES = ['None', 'Overlay', 'Outline']
COLORS = {
    'Red': '#944444', 'Green': '#44945d', 'Blue': '#446e94', 
    'Yellow': '#948b44', 'Cyan': '#449494', 'Magenta': "#864494", 
    'White': '#d1d5db', 'Black': '#334155'
}

# --- UI Styling ---
ui.query('body').style('background-color: #0f172a; font-family: "Inter", sans-serif;')
CARD_STYLE = 'w-full max-w-md bg-[#1e293b] text-slate-300 rounded-xl border border-slate-800 p-6'

with ui.column().classes('w-full items-center p-8 gap-6'):
    ui.label('DESIGN INTERFACE').classes('text-xs tracking-[.3em] text-slate-500 font-bold')
    
    # 1. Cue Type
    with ui.card().classes(CARD_STYLE):
        ui.label('Visual Cue').classes('text-xs font-bold text-slate-500 mb-2')
        ui.toggle(CUE_TYPES, value='None', 
                  on_change=lambda e: send_update("cue_type", e.value)).classes('w-full').props('no-caps spread unelevated toggle-color=slate-700 color=slate-800 text-color=slate-400')

    # 2. Size
    with ui.card().classes(CARD_STYLE):
        ui.label('Object Scale').classes('text-xs font-bold text-slate-500 mb-2')
        ui.select(SIZES, value='Medium', on_change=lambda e: send_update("size", e.value)).classes('w-full').props('dark dense outlined color=slate-600')

    # 3. Muted Color Palette
    with ui.card().classes(CARD_STYLE):
        ui.label('Color Palette').classes('text-xs font-bold text-slate-500 mb-4')
        with ui.grid(columns=4).classes('w-full gap-4'):
            for name, hex_val in COLORS.items():
                ui.button(on_click=lambda h=hex_val: send_update("color", h)) \
                    .style(f'background-color: {hex_val}; height: 40px; width: 40px; border-radius: 50%; border: 2px solid #334155;') \
                    .props('flat').tooltip(name)

    # 4. Alpha (Snapping Slider)
    with ui.card().classes(CARD_STYLE):
        ui.label('Alpha Opacity').classes('text-xs font-bold text-slate-500 mb-2')
        ui.slider(min=0, max=1, step=0.25, value=1.0, 
                  on_change=lambda e: send_update("alpha", e.value)).props('label-always color=slate-400 markers')

ui.run(title="Visual Cue Control Panel", port=UNITY_PORT, dark=True)