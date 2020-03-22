using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tiwi.Sockets
{
    public abstract class WebSocketHandler
    {
        protected ConnectionManager WebSocketConnectionManager { get; set; }

        public WebSocketHandler(ConnectionManager webSocketConnectionManager)
        {
            this.WebSocketConnectionManager = webSocketConnectionManager;
        }

        public virtual async Task OnConnectedAsync(WebSocket socket,
                                                   TaskCompletionSource<object?> socketFinishedTcs,
                                                   CancellationToken cancellationToken)
        {
            this.WebSocketConnectionManager.AddSocket(socket, socketFinishedTcs);
            await Task.CompletedTask;
        }

        public virtual async Task OnDisconnectedAsync(WebSocket socket, CancellationToken cancellationToken) =>
            await this.WebSocketConnectionManager.RemoveSocketAsync(this.WebSocketConnectionManager.GetId(socket), cancellationToken);

        public async Task SendMessageAsync(WebSocket socket, string message, CancellationToken cancellationToken)
        {
            if (socket.State != WebSocketState.Open)
                return;

            try
            {
                var buffer = new ArraySegment<byte>(
                    array: Encoding.ASCII.GetBytes(message),
                    offset: 0,
                    count: message.Length);

                await socket.SendAsync(buffer: buffer,
                                       messageType: WebSocketMessageType.Text,
                                       endOfMessage: true,
                                       cancellationToken: cancellationToken);
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                await this.OnDisconnectedAsync(socket, cancellationToken);
            }
        }

        public async Task SendMessageAsync(Guid socketId, string message, CancellationToken cancellationToken)
        {
            var socket = this.WebSocketConnectionManager.GetSocketById(socketId);
            if (socket != null)
            {
                await this.SendMessageAsync(socket, message, cancellationToken);
            }
        }

        public async Task SendMessageToAllAsync(string message, CancellationToken cancellationToken)
        {
            foreach (var socket in this.WebSocketConnectionManager.GetAll())
            {
                if (socket.State == WebSocketState.Open)
                {
                    await this.SendMessageAsync(socket, message, cancellationToken);
                }
            }
        }
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, Stream message, CancellationToken cancellationToken);
    }
}
