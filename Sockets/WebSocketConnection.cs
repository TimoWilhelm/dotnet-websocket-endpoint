using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class WebSocketConnection
    {
        public WebSocketConnection(WebSocket webSocket, TaskCompletionSource<object?> socketFinishedTcs)
        {
            this.Id = Guid.NewGuid();
            this.WebSocket = webSocket;
            this.SocketFinishedTcs = socketFinishedTcs;
        }

        public Guid Id { get; set; }
        public WebSocket WebSocket { get; set; }
        public TaskCompletionSource<object?> SocketFinishedTcs { get; set; }
    }
}
