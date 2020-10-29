using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace EasyWebSockets
{
    internal class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public WebSocket GetSocketById(string id) => 
            _sockets.FirstOrDefault(p => p.Key == id).Value;

        public ConcurrentDictionary<string, WebSocket> GetAll() => _sockets;

        public string GetId(WebSocket socket) => _sockets.FirstOrDefault(p => p.Value == socket).Key;

        public void AddSocket(WebSocket socket) => _sockets.TryAdd(CreateConnectionId(), socket);

        public Task RemoveSocket(string id, CancellationToken cancellationToken = default)
        {
            _sockets.TryRemove(id, out WebSocket socket);

            return socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure, 
                                    statusDescription: "Closed by the WebSocketManager", 
                                    cancellationToken: cancellationToken);
        }

        private static string CreateConnectionId() => Guid.NewGuid().ToString();
    }
}