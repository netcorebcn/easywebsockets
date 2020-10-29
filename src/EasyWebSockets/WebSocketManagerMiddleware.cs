using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EasyWebSockets
{
    internal class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketHandler _webSocketHandler;

        public WebSocketManagerMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            WebSocket? socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket, context.RequestAborted);
            await Receive(socket, async (result, serializedInvocationDescriptor) =>
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        // await _webSocketHandler.ReceiveAsync(socket, result, serializedInvocationDescriptor);
                        return;

                    case WebSocketMessageType.Close:
                        await _webSocketHandler.OnDisconnected(socket, context.RequestAborted);
                        return;
                }
            }, context.RequestAborted);
        }

        private static async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, string> handleMessage, CancellationToken cancellationToken = default)
        {
            while (socket.State == WebSocketState.Open)
            {
                var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
                string serializedInvocationDescriptor;
                WebSocketReceiveResult result;

                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, cancellationToken);
                        await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count, cancellationToken);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    serializedInvocationDescriptor = await reader.ReadToEndAsync();
                }

                handleMessage(result, serializedInvocationDescriptor);
            }
        }
    }
}