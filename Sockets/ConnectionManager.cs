using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, SocketConnection> sockets = new ConcurrentDictionary<Guid, SocketConnection>();

        public int NumberOfConnections => this.sockets.Count;

        public WebSocket? GetSocketById(Guid id) => this.sockets.FirstOrDefault(c => c.Key == id).Value.Socket;

        public IEnumerable<WebSocket> GetAll()
        {
            return this.sockets.Values
                .Select(c => c.Socket)
                .Where(s => s != null)
                .Cast<WebSocket>();
        }

        public Guid GetId(WebSocket socket) => this.sockets.FirstOrDefault(c => c.Value.Socket == socket).Key;
        public void AddSocket(WebSocket socket, TaskCompletionSource<object?> socketFinishedTcs)
        {
            var socketConnection = new SocketConnection
            {
                Id = Guid.NewGuid(),
                Socket = socket,
                SocketFinishedTcs = socketFinishedTcs
            };
            this.sockets.TryAdd(socketConnection.Id, socketConnection);
        }

        public async Task RemoveSocketAsync(Guid id, CancellationToken cancellationToken)
        {
            if (this.sockets.TryRemove(id, out var c))
            {
                try
                {
                    await (c.Socket?.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                            statusDescription: "Closed by the ConnectionManager",
                                            cancellationToken: cancellationToken) ?? Task.CompletedTask);
                }
                catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely) { }
                finally
                {
                    c.SocketFinishedTcs?.TrySetResult(null);
                }
            }
        }
    }
}
