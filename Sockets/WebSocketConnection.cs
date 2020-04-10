using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class WebSocketConnection
    {
        public WebSocketConnection(WebSocket webSocket, TaskCompletionSource<bool> socketFinishedTcs)
        {
            this.Id = Guid.NewGuid();
            this.WebSocket = webSocket;
            this.SocketFinishedTcs = socketFinishedTcs;
        }

        public Guid Id { get; }
        public WebSocket WebSocket { get; }
        public TaskCompletionSource<bool> SocketFinishedTcs { get; }
    }
}
