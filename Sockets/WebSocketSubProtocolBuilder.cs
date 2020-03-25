using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Tiwi.Sockets
{
    public class WebSocketSubProtocolBuilder
    {
        private readonly IServiceProvider serviceProvider;
        private readonly List<WebSocketProtocolHandler> registeredProtocols = new List<WebSocketProtocolHandler>();

        public WebSocketSubProtocolBuilder(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public WebSocketSubProtocolBuilder AddSubProtocol<T>() where T : WebSocketProtocolHandler
        {
            this.registeredProtocols.Add(this.serviceProvider.GetRequiredService<T>());
            return this;
        }

        public WebSocketSubProtocolProvider Build() => new WebSocketSubProtocolProvider(this.registeredProtocols);
    }
}
