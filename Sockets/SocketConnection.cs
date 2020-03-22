using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public class SocketConnection
    {
        public Guid Id { get; set; }
        public WebSocket? Socket { get; set; }
        public TaskCompletionSource<object?>? SocketFinishedTcs { get; set; }
    }
}
