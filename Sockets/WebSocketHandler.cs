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
        protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
        {
            this.WebSocketConnectionManager = webSocketConnectionManager;
        }

        public abstract Task OnConnectedAsync(Guid socketId, CancellationToken cancellationToken);

        public abstract Task OnDisconnectedAsync(Guid socketId);

        public abstract Task ReceiveAsync(Guid socketId, WebSocketReceiveResult result, Stream message, CancellationToken cancellationToken);

        internal async Task<Guid> AddConnectionAsync(WebSocket webSocket,
                                                     TaskCompletionSource<object?> socketFinishedTcs,
                                                     CancellationToken cancellationToken)
        {
            var socketConnection = this.WebSocketConnectionManager.AddSocket(webSocket, socketFinishedTcs);
            await this.OnConnectedAsync(socketConnection.Id, cancellationToken);

            return socketConnection.Id;
        }

        public async Task CloseConnectionAsync(Guid socketId, string statusDescription, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            if (this.WebSocketConnectionManager.TryRemoveSocket(socketId, out var socketConnection))
            {
                try
                {
                    if (socketConnection.WebSocket.State != WebSocketState.Closed)
                    {
                        try
                        {
                            await socketConnection.WebSocket.CloseAsync(closeStatus: closeStatus,
                                                                        statusDescription: statusDescription,
                                                                        cancellationToken: CancellationToken.None);
                        }
                        catch (WebSocketException)
                        {
                            socketConnection.WebSocket.Abort();
                        }
                    }
                }
                finally
                {
                    socketConnection.SocketFinishedTcs.TrySetResult(null);
                    await this.OnDisconnectedAsync(socketId);
                }
            }
        }

        internal async Task CloseConnectionAsync(WebSocket webSocket, string statusDescription, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure)
        {
            var socketId = this.WebSocketConnectionManager.GetId(webSocket);
            await this.CloseConnectionAsync(socketId, statusDescription, closeStatus);
        }

        public async Task SendMessageAsync(Guid socketId, string message, CancellationToken cancellationToken)
        {
            if (this.WebSocketConnectionManager.TryGetSocketById(socketId, out var socketConnection))
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await this.SendMessageAsync(socketConnection.WebSocket, messageBytes, WebSocketMessageType.Text, cancellationToken);
            }
        }

        public async Task SendMessageAsync(Guid socketId, byte[] message, CancellationToken cancellationToken)
        {
            if (this.WebSocketConnectionManager.TryGetSocketById(socketId, out var socketConnection))
            {
                await this.SendMessageAsync(socketConnection.WebSocket, message, WebSocketMessageType.Binary, cancellationToken);
            }
        }

        public async Task SendMessageToAllAsync(string message, CancellationToken cancellationToken)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            foreach (var socket in this.WebSocketConnectionManager.GetAll())
            {
                if (socket.State == WebSocketState.Open)
                {
                    await this.SendMessageAsync(socket, messageBytes, WebSocketMessageType.Text, cancellationToken);
                }
            }
        }

        public async Task SendMessageToAllAsync(byte[] message, CancellationToken cancellationToken)
        {
            foreach (var socket in this.WebSocketConnectionManager.GetAll())
            {
                if (socket.State == WebSocketState.Open)
                {
                    await this.SendMessageAsync(socket, message, WebSocketMessageType.Binary, cancellationToken);
                }
            }
        }

        private async Task SendMessageAsync(WebSocket webSocket, byte[] message, WebSocketMessageType messageType, CancellationToken cancellationToken)
        {
            if (webSocket.State != WebSocketState.Open)
                return;

            try
            {
                int chunkSize = 8192;

                for (int i = 0; i < message.Length; i += chunkSize)
                {
                    bool endOfMessage = i + chunkSize > message.Length;

                    int count = endOfMessage ? message.Length - i : chunkSize;
                    var chunk = new ArraySegment<byte>(message, i, count);

                    await webSocket.SendAsync(buffer: chunk,
                                              messageType: messageType,
                                              endOfMessage: endOfMessage,
                                              cancellationToken: cancellationToken);
                }
            }
            catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                await this.CloseConnectionAsync(webSocket, "Connection Closed Prematurely");
            }
        }
    }
}
