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

        public abstract Task OnConnectedAsync(Guid socketId, CancellationToken cancellationToken);

        public abstract Task OnDisconnectedAsync(Guid socketId);

        public abstract Task ReceiveAsync(Guid socketId, WebSocketReceiveResult result, Stream message, CancellationToken cancellationToken);

        internal async Task<Guid> AddConnectionAsync(WebSocket socket,
                                             TaskCompletionSource<object?> socketFinishedTcs,
                                             CancellationToken cancellationToken)
        {
            var socketId = this.WebSocketConnectionManager.AddSocket(socket, socketFinishedTcs);
            await this.OnConnectedAsync(socketId, cancellationToken);
            return socketId;
        }

        public async Task CloseConnectionAsync(Guid socketId, string statusDescription, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            if (this.WebSocketConnectionManager.TryRemoveSocket(socketId, out var socketConnection) && socketConnection.Socket != null)
            {
                try
                {
                    if (socketConnection.Socket.State != WebSocketState.Closed)
                    {
                        try
                        {
                            await (socketConnection.Socket.CloseAsync(closeStatus: closeStatus,
                                               statusDescription: statusDescription,
                                               cancellationToken: CancellationToken.None) ?? Task.CompletedTask);
                        }
                        catch (WebSocketException)
                        {
                            socketConnection.Socket.Abort();
                        }
                    }
                }
                finally
                {
                    socketConnection.SocketFinishedTcs?.TrySetResult(null);
                    await this.OnDisconnectedAsync(socketId);
                }
            }
        }

        internal async Task CloseConnectionAsync(WebSocket socket, string statusDescription, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            var socketId = this.WebSocketConnectionManager.GetId(socket);
            await this.CloseConnectionAsync(socketId, statusDescription, closeStatus);
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

        private async Task SendMessageAsync(WebSocket socket, string message, CancellationToken cancellationToken)
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
                await this.CloseConnectionAsync(socket, "Connection Closed Prematurely");
            }
        }
    }
}
