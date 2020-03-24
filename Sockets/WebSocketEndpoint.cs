using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Tiwi.Sockets
{
    public class WebSocketEndpoint
    {
        public async Task HandleRequestAsync(HttpContext context, WebSocketHandler webSocketHandler)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                context.Response.StatusCode = 400;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var socketFinishedTcs = new TaskCompletionSource<object?>();

            var socketId = await webSocketHandler.AddConnectionAsync(socket, socketFinishedTcs, context.RequestAborted);

            WebSocketReceiveResult? result = null;
            _ = Task.Run(async () =>
            {
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        result = await this.ReadMessageAsync(socket, ms, context.RequestAborted);

                        if (result.MessageType == WebSocketMessageType.Binary || result.MessageType == WebSocketMessageType.Text)
                        {
                            await webSocketHandler.ReceiveAsync(socketId, result, ms, context.RequestAborted);
                        }

                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocketHandler.CloseConnectionAsync(socket, "Connection Closed");
                        }
                    }
                    catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await webSocketHandler.CloseConnectionAsync(socket, "Connection Closed Prematurely");
                        return;
                    }
                    catch (TaskCanceledException) when (socket.State == WebSocketState.Aborted)
                    {
                        await webSocketHandler.CloseConnectionAsync(socket, "WebSocket Aborted");
                        return;
                    }
                }
            });

            await socketFinishedTcs.Task;
        }

        private async Task<WebSocketReceiveResult> ReadMessageAsync(WebSocket webSocket, Stream messageStream, CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult? result;
            do
            {
                result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                await messageStream.WriteAsync(buffer.Array ?? Array.Empty<byte>(), buffer.Offset, result.Count, cancellationToken);
            }
            while (!result.EndOfMessage);

            messageStream.Seek(0, SeekOrigin.Begin);
            return result;
        }
    }
}
