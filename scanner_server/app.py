from flask import Flask, render_template, request
from flask_socketio import SocketIO
import eventlet

# eventlet provides async support for Flask-SocketIO
eventlet.monkey_patch()

app = Flask(__name__, template_folder='templates', static_folder='static')
socketio = SocketIO(app, cors_allowed_origins="*")


@app.route('/')
def index():
    return 'Barcode scanner server running. Open /scanner on a phone.'


@app.route('/scanner')
def scanner():
    return render_template('scanner.html')


@socketio.on('connect')
def on_connect():
    addr = request.remote_addr
    print(f'Client connected: {addr}')


@socketio.on('disconnect')
def on_disconnect():
    addr = request.remote_addr
    print(f'Client disconnected: {addr}')


@socketio.on('scan')
def on_scan(data):
    # Received from phone scanner page
    code = None
    if isinstance(data, dict):
        code = data.get('code')
    else:
        code = data

    if code:
        print(f"Scanned code received: {code}")
        # Forward to any connected POS clients as a 'barcode' event
        socketio.emit('barcode', {'code': code}, broadcast=True)
    return {'status': 'ok'}


if __name__ == '__main__':
    # Listen on all interfaces so phones on the same LAN can connect
    socketio.run(app, host='0.0.0.0', port=5000)
