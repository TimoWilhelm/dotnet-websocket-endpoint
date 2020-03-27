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
        public async Task HandleRequestAsync(HttpContext context, WebSocketSubProtocolProvider protocolProvider)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            if (!protocolProvider.TryNegotiateSubProtocol(context.WebSockets.WebSocketRequestedProtocols, out var protocolHandler))
            {
                // TODO Beter error message
                context.Response.StatusCode = 400;
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync(protocolHandler.SubProtocolIdentifier);
            var socketFinishedTcs = new TaskCompletionSource<bool>();
            var socketId = await protocolHandler.AddConnectionAsync(socket, socketFinishedTcs, context.RequestAborted);

            WebSocketReceiveResult? result = null;
            _ = Task.Run(async () =>
            {
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        result = await this.ReadFullMessageAsync(socket, ms, context.RequestAborted);

                        if (result.MessageType == WebSocketMessageType.Binary || result.MessageType == WebSocketMessageType.Text)
                        {
                            await protocolHandler.ReceiveAsync(socketId, result, ms, context.RequestAborted);
                        }

                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await protocolHandler.CloseConnectionAsync(socket, result.CloseStatus ?? WebSocketCloseStatus.Empty, result.CloseStatusDescription);
                        }
                    }
                    catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        await protocolHandler.CloseConnectionAsync(socket, WebSocketCloseStatus.Empty, "Connection Closed Prematurely");
                        return;
                    }
                    catch (TaskCanceledException) when (socket.State == WebSocketState.Aborted)
                    {
                        await protocolHandler.CloseConnectionAsync(socket, WebSocketCloseStatus.Empty, "WebSocket Aborted");
                        return;
                    }
                }
            });

            await socketFinishedTcs.Task;
        }

        private async Task<WebSocketReceiveResult> ReadFullMessageAsync(WebSocket webSocket, Stream messageStream, CancellationToken cancellationToken)
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
