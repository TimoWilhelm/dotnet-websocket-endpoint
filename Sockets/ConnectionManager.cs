using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, SocketConnection> sockets = new ConcurrentDictionary<Guid, SocketConnection>();

        public int ConnectionCount => this.sockets.Count;

        public WebSocket? GetSocketById(Guid id) => this.sockets.FirstOrDefault(c => c.Key == id).Value.Socket;

        public IEnumerable<WebSocket> GetAll()
        {
            return this.sockets.Values
                .Select(c => c.Socket)
                .Where(s => s != null)
                .Cast<WebSocket>();
        }

        public Guid GetId(WebSocket socket) => this.sockets.FirstOrDefault(c => c.Value.Socket == socket).Key;
        public Guid AddSocket(WebSocket socket, TaskCompletionSource<object?> socketFinishedTcs)
        {
            var socketConnection = new SocketConnection
            {
                Id = Guid.NewGuid(),
                Socket = socket,
                SocketFinishedTcs = socketFinishedTcs
            };

            this.sockets.TryAdd(socketConnection.Id, socketConnection);

            return socketConnection.Id;
        }

        public bool TryRemoveSocket(Guid id, [MaybeNullWhen(false)] out SocketConnection socketConnection) => this.sockets.TryRemove(id, out socketConnection);
    }
}
