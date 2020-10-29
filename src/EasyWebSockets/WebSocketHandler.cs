using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EasyWebSockets
{
    public interface IWebSocketPublisher
    {
        Task SendMessageToAllAsync(object message, CancellationToken cancellationToken = default);
    }

    internal class WebSocketHandler : IWebSocketPublisher
    {
        private readonly WebSocketConnectionManager _webSocketConnectionManager;

        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager) =>
            _webSocketConnectionManager = webSocketConnectionManager;

        public Task SendMessageToAllAsync(object message, CancellationToken cancellationToken = default) =>
            Task.WhenAll(_webSocketConnectionManager.GetAll()
                    .Where(pair => pair.Value.State == WebSocketState.Open)
                    .Select(pair => SendMessageAsync(pair.Value, message, cancellationToken)));

        public Task OnConnected(WebSocket socket, CancellationToken cancellationToken = default)
        {
            _webSocketConnectionManager.AddSocket(socket);
            return SendMessageAsync(socket, $"Connected with Id: ${_webSocketConnectionManager.GetId(socket)}", cancellationToken);
        }

        public Task OnDisconnected(WebSocket socket, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _webSocketConnectionManager.RemoveSocket(_webSocketConnectionManager.GetId(socket), cancellationToken);
        }

        private async Task SendMessageAsync(WebSocket socket, object message, CancellationToken cancellationToken = default)
        {
            if (socket.State != WebSocketState.Open)
                return;

            string? serializedMessage = JsonConvert.SerializeObject(message, _jsonSerializerSettings);
            await socket.SendAsync(buffer: new ArraySegment<byte>(
                    array: Encoding.ASCII.GetBytes(serializedMessage),
                    offset: 0,
                    count: serializedMessage.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: cancellationToken);
        }
    }
}