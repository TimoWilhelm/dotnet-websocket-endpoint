
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tiwi.Sockets.Examples.Chat
{
    public class ChatMessageProtocolHandler : WebSocketProtocolHandler
    {
        public override string SubProtocolIdentifier => "tiwi.example.chat";
        public ChatMessageProtocolHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager) { }

        public override async Task OnConnectedAsync(Guid socketId, CancellationToken cancellationToken) =>
            await this.SendMessageToAllAsync($"{socketId} is now connected", cancellationToken);

        public override async Task OnDisconnectedAsync(Guid socketId, WebSocketCloseStatus closeStatus, string closeStatusDescription) =>
            await this.SendMessageToAllAsync($"{socketId} disconnected", CancellationToken.None);

        public override async Task ReceiveAsync(Guid socketId, WebSocketReceiveResult result, Stream message, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(message, Encoding.UTF8);
            string chatMessage = $"{socketId} said: {await reader.ReadToEndAsync()}";

            await this.SendMessageToAllAsync(chatMessage, cancellationToken);
        }
    }

}
