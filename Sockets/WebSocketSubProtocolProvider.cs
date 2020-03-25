using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Tiwi.Sockets
{
    public class WebSocketSubProtocolProvider
    {
        private readonly IList<WebSocketProtocolHandler> registeredProtocols;

        public WebSocketSubProtocolProvider(IList<WebSocketProtocolHandler> registeredProtocols)
        {
            this.registeredProtocols = registeredProtocols;
        }

        public bool TryNegotiateSubProtocol(IList<string> webSocketRequestedProtocols, [MaybeNullWhen(false)] out WebSocketProtocolHandler subProtocolHandler)
        {
            subProtocolHandler = this.registeredProtocols.FirstOrDefault(p => webSocketRequestedProtocols.Contains(p.SubProtocolIdentifier));
            return subProtocolHandler != null;
        }
    }
}
