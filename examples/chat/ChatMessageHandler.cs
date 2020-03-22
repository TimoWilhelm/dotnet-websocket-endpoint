
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tiwi.Sockets.Examples.Chat
{
    public class ChatMessageHandler : WebSocketHandler
    {
        public ChatMessageHandler(ConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager) { }
        public override async Task OnConnectedAsync(WebSocket socket, TaskCompletionSource<object?> socketFinishedTcs, CancellationToken cancellationToken)
        {
            await base.OnConnectedAsync(socket, socketFinishedTcs, cancellationToken);

            var socketId = this.WebSocketConnectionManager.GetId(socket);
            await this.SendMessageToAllAsync($"{socketId} is now connected", cancellationToken);
        }

        public override async Task OnDisconnectedAsync(WebSocket socket)
        {
            var socketId = this.WebSocketConnectionManager.GetId(socket);

            await base.OnDisconnectedAsync(socket);

            await this.SendMessageToAllAsync($"{socketId} disconnected", CancellationToken.None);
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, Stream message, CancellationToken cancellationToken)
        {
            var socketId = this.WebSocketConnectionManager.GetId(socket);

            using var reader = new StreamReader(message, Encoding.UTF8);
            string chatMessage = $"{socketId} said: {await reader.ReadToEndAsync()}";

            await this.SendMessageToAllAsync(chatMessage, cancellationToken);
        }
    }

}
