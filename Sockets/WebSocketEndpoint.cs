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
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var socketFinishedTcs = new TaskCompletionSource<object?>();

            await webSocketHandler.OnConnectedAsync(socket, socketFinishedTcs, context.RequestAborted);

            WebSocketReceiveResult? result = null;
            _ = Task.Run(async () =>
            {
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        result = await this.ReadMessageAsync(socket, ms, context.RequestAborted);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            await webSocketHandler.ReceiveAsync(socket, result, ms, context.RequestAborted);
                        }

                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocketHandler.OnDisconnectedAsync(socket);
                        }
                    }
                    catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        Console.WriteLine("Connection Closed Prematurely");
                        await webSocketHandler.OnDisconnectedAsync(socket);
                        return;
                    }
                    catch (TaskCanceledException) when (socket.State == WebSocketState.Aborted)
                    {
                        Console.WriteLine("WebSocket Aborted");
                        await webSocketHandler.OnDisconnectedAsync(socket);
                        return;
                    }
                }
            });

            await socketFinishedTcs.Task;
        }

        private async Task<WebSocketReceiveResult> ReadMessageAsync(WebSocket socket, Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            WebSocketReceiveResult? result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                await stream.WriteAsync(buffer.Array ?? Array.Empty<byte>(), buffer.Offset, result.Count, cancellationToken);
            }
            while (!result.EndOfMessage);

            stream.Seek(0, SeekOrigin.Begin);
            return result;
        }
    }
}
