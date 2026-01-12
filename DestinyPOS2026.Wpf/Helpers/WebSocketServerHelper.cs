using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DestinyPOS2026.Wpf.Helpers;

public static class WebSocketServerHelper
{
    private static HttpListener? _listener;

    public static async Task StartServer(int port, string sessionToken, Action<string> onBarcodeReceived)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/ws/");
        _listener.Start();

        while (true)
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                // Check token
                var token = context.Request.QueryString["token"];
                if (token != sessionToken)
                {
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(null);
                _ = HandleClient(wsContext.WebSocket, onBarcodeReceived);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task HandleClient(WebSocket webSocket, Action<string> onBarcodeReceived)
    {
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                onBarcodeReceived?.Invoke(msg);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}
