using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, WebSocketConnection> socketConnections = new ConcurrentDictionary<Guid, WebSocketConnection>();

        public int ConnectionCount => this.socketConnections.Count;

        internal bool TryGetSocketById(Guid id, [MaybeNullWhen(false)] out WebSocketConnection socketConnection) =>
            this.socketConnections.TryGetValue(id, out socketConnection);


        internal IEnumerable<WebSocket> GetAll()
        {
            return this.socketConnections.Values
                .Select(c => c.WebSocket);
        }

        internal Guid GetId(WebSocket webSocket) => this.socketConnections.First(c => c.Value.WebSocket == webSocket).Key;

        internal WebSocketConnection AddSocket(WebSocket webSocket, TaskCompletionSource<object?> socketFinishedTcs)
        {
            var socketConnection = new WebSocketConnection(webSocket, socketFinishedTcs);
            this.socketConnections.TryAdd(socketConnection.Id, socketConnection);

            return socketConnection;
        }

        internal bool TryRemoveSocket(Guid id, [MaybeNullWhen(false)] out WebSocketConnection socketConnection) =>
            this.socketConnections.TryRemove(id, out socketConnection);
    }
}
