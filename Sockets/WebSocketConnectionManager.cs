using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class WebSocketConnectionManager : IEnumerable<WebSocketConnection>
    {
        private readonly ConcurrentDictionary<Guid, WebSocketConnection> socketConnections = new ConcurrentDictionary<Guid, WebSocketConnection>();

        public int ConnectionCount => this.socketConnections.Count;

        internal bool TryGetSocketById(Guid id, [MaybeNullWhen(false)] out WebSocketConnection socketConnection) =>
            this.socketConnections.TryGetValue(id, out socketConnection);

        internal Guid GetId(WebSocket webSocket) => this.socketConnections.First(c => c.Value.WebSocket == webSocket).Key;

        internal WebSocketConnection AddSocket(WebSocket webSocket, TaskCompletionSource<bool> socketFinishedTcs)
        {
            var socketConnection = new WebSocketConnection(webSocket, socketFinishedTcs);
            this.socketConnections.TryAdd(socketConnection.Id, socketConnection);

            return socketConnection;
        }

        internal bool TryRemoveSocket(Guid id, [MaybeNullWhen(false)] out WebSocketConnection socketConnection) =>
            this.socketConnections.TryRemove(id, out socketConnection);

        public IEnumerator<WebSocketConnection> GetEnumerator() => this.socketConnections.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.socketConnections.Values.GetEnumerator();
    }
}
