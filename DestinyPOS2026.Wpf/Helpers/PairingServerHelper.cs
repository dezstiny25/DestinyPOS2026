using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DestinyPOS2026.Wpf.Helpers;

/// <summary>
/// Minimal HTTP + WebSocket server for phone pairing.
/// Serves a pairing page on GET / and handles barcode reception via WebSocket /ws.
/// </summary>
public static class PairingServerHelper
{
    private static HttpListener? _listener;
    private static CancellationTokenSource? _cts;
    public static event Action<string>? BarcodeReceived;
    public static event Action<string>? StatusChanged;

    /// <summary>
    /// Start the pairing server on the next available port, bound to the local LAN IP.
    /// </summary>
    public static async Task StartAsync(string? sessionToken = null)
    {
        if (_listener != null) return; // Already running

        if (!HttpListener.IsSupported)
            throw new NotSupportedException("HttpListener is not supported on this platform.");

        sessionToken ??= Guid.NewGuid().ToString();
        var port = FindAvailablePort();
        var ip = NetworkHelper.GetLocalIPAddress();
        var prefix = $"http://{ip}:{port}/";

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        try
        {
            _listener.Start();
            var statusMsg = $"Pairing server listening on {prefix}";
            StatusChanged?.Invoke(statusMsg);
            Console.WriteLine($"[Pairing] {statusMsg}");
            
            // Write pairing URL to file for easy access
            try
            {
                System.IO.File.WriteAllText("pairing_url.txt", prefix);
            }
            catch { }
        }
        catch (HttpListenerException ex)
        {
            StatusChanged?.Invoke($"Failed to start pairing server: {ex.Message}");
            throw;
        }

        _cts = new CancellationTokenSource();
        _ = HandleRequestsAsync(sessionToken, _cts.Token);
    }

    /// <summary>
    /// Stop the pairing server.
    /// </summary>
    public static void Stop()
    {
        if (_listener != null)
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
        }
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        StatusChanged?.Invoke("Pairing server stopped.");
    }

    /// <summary>
    /// Get the current server prefix (e.g., "http://192.168.1.100:5050/").
    /// </summary>
    public static string? GetServerPrefix()
    {
        if (_listener?.IsListening == true && _listener.Prefixes.Count > 0)
            return _listener.Prefixes.GetEnumerator().Current;
        return null;
    }

    private static async Task HandleRequestsAsync(string sessionToken, CancellationToken ct)
    {
        while (_listener?.IsListening == true && !ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context, sessionToken);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pairing] Error handling request: {ex.Message}");
            }
        }
    }

    private static async Task ProcessRequestAsync(HttpListenerContext context, string sessionToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/")
            {
                // Serve pairing page with QR code and WebSocket client
                var html = GeneratePairingPage(sessionToken);
                var buffer = Encoding.UTF8.GetBytes(html);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
                return;
            }

            if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/ws")
            {
                if (!context.Request.IsWebSocketRequest)
                {
                    response.StatusCode = 400;
                    response.Close();
                    return;
                }

                var wsContext = await context.AcceptWebSocketAsync(null);
                var ws = wsContext.WebSocket;
                await HandleWebSocketAsync(ws, sessionToken);
                return;
            }

            // 404
            response.StatusCode = 404;
            response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Pairing] Error processing request: {ex.Message}");
            try { response.Close(); } catch { }
        }
    }

    private static async Task HandleWebSocketAsync(WebSocket ws, string sessionToken)
    {
        var buffer = new byte[4096];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.CloseStatus.HasValue)
                {
                    await ws.CloseAsync(
                        result.CloseStatus.Value,
                        result.CloseStatusDescription,
                        CancellationToken.None);
                    break;
                }

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessWebSocketMessage(msg, sessionToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Pairing] WebSocket error: {ex.Message}");
        }
        finally
        {
            ws?.Dispose();
        }
    }

    private static void ProcessWebSocketMessage(string msg, string sessionToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;

            // Validate token
            var token = root.TryGetProperty("token", out var t) ? t.GetString() : null;
            if (token != sessionToken)
            {
                Console.WriteLine($"[Pairing] Invalid token: {token}");
                return;
            }

            // Extract barcode
            var barcode = root.TryGetProperty("barcode", out var b) ? b.GetString() : null;
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                Console.WriteLine($"[Pairing] Received barcode: {barcode}");
                BarcodeReceived?.Invoke(barcode);
            }
        }
        catch (JsonException)
        {
            Console.WriteLine($"[Pairing] Failed to parse JSON: {msg}");
        }
    }

    private static string GeneratePairingPage(string sessionToken)
    {
        var serverPrefix = GetServerPrefix() ?? "http://localhost/";
        var wsUrl = "ws://" + new Uri(serverPrefix).Authority + "/ws";
        var pageUrl = serverPrefix;

        // HTML page with embedded styling and QR code generation
        var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>DestinyPOS - Phone Scanner Pairing</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .container { 
            background: white;
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 500px;
            padding: 40px;
            text-align: center;
        }
        h1 { 
            font-size: 28px;
            color: #333;
            margin-bottom: 10px;
        }
        .subtitle {
            color: #666;
            font-size: 14px;
            margin-bottom: 30px;
        }
        #qr {
            margin: 30px auto;
            padding: 20px;
            background: #f5f5f5;
            border-radius: 8px;
            display: flex;
            justify-content: center;
            min-height: 290px;
        }
        #qr canvas, #qr img {
            max-width: 100%;
            height: auto;
        }
        .manual-input {
            margin-top: 30px;
            padding-top: 30px;
            border-top: 1px solid #eee;
        }
        .manual-input p {
            color: #666;
            font-size: 13px;
            margin-bottom: 10px;
        }
        input {
            width: 100%;
            padding: 12px;
            font-size: 16px;
            border: 2px solid #ddd;
            border-radius: 6px;
            transition: border-color 0.3s;
        }
        input:focus {
            outline: none;
            border-color: #667eea;
        }
        button {
            width: 100%;
            padding: 12px;
            margin-top: 10px;
            font-size: 16px;
            font-weight: 600;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            transition: background 0.3s;
        }
        button:hover {
            background: #5568d3;
        }
        button:active {
            background: #4859ba;
        }
        #status {
            margin-top: 20px;
            padding: 12px;
            border-radius: 6px;
            font-weight: 600;
            font-size: 13px;
        }
        #status.connecting {
            background: #fff3cd;
            color: #856404;
        }
        #status.connected {
            background: #d4edda;
            color: #155724;
        }
        #status.error {
            background: #f8d7da;
            color: #721c24;
        }
    </style>
</head>
<body>
    <div class='container'>
        <h1>DestinyPOS</h1>
        <p class='subtitle'>Phone Scanner Pairing</p>
        
        <div id='qr'><p style='color: #999;'>Generating QR code...</p></div>
        
        <div class='manual-input'>
            <p>Or enter barcode manually:</p>
            <input type='text' id='barcode' placeholder='Scan or type barcode' autofocus />
            <button onclick='sendBarcode()'>Send</button>
        </div>
        
        <div id='status' class='connecting'>Connecting...</div>
    </div>

    <script src='https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js'></script>
    <script>
        const token = '" + sessionToken + @"';
        const wsUrl = '" + wsUrl + @"';
        const pageUrl = '" + pageUrl + @"';
        let ws = null;

        // Generate QR code
        window.onload = () => {
            try {
                const qrContainer = document.getElementById('qr');
                qrContainer.innerHTML = '';
                new QRCode(qrContainer, {
                    text: pageUrl,
                    width: 250,
                    height: 250,
                    correctLevel: QRCode.CorrectLevel.H
                });
            } catch (e) {
                console.error('QR code generation failed:', e);
                document.getElementById('qr').innerHTML = '<p style=""color: #999; font-size: 12px;"">QR code unavailable<br><small>URL: ' + pageUrl + '</small></p>';
            }
            connectWebSocket();
        };

        function connectWebSocket() {
            try {
                ws = new WebSocket(wsUrl);
                ws.onopen = () => {
                    updateStatus('Connected', 'connected');
                };
                ws.onerror = (e) => {
                    updateStatus('Connection error', 'error');
                };
                ws.onclose = () => {
                    updateStatus('Reconnecting...', 'connecting');
                    setTimeout(connectWebSocket, 3000);
                };
            } catch (e) {
                console.error('WebSocket error:', e);
                updateStatus('WebSocket unavailable', 'error');
            }
        }

        function sendBarcode() {
            const input = document.getElementById('barcode');
            const barcode = input.value.trim();
            if (!barcode) return;

            if (ws && ws.readyState === WebSocket.OPEN) {
                try {
                    ws.send(JSON.stringify({ token: token, barcode: barcode }));
                    input.value = '';
                    input.focus();
                    updateStatus('Barcode sent', 'connected');
                } catch (e) {
                    console.error('Send error:', e);
                    updateStatus('Send failed', 'error');
                }
            } else {
                updateStatus('Not connected', 'error');
            }
        }

        function updateStatus(msg, className) {
            const el = document.getElementById('status');
            el.textContent = msg;
            el.className = className;
        }

        document.getElementById('barcode').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                sendBarcode();
            }
        });
    </script>
</body>
</html>";

        return html;
    }

    private static int FindAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
