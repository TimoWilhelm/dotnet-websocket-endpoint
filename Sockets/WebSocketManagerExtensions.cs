using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tiwi.Sockets
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<WebSocketConnectionManager>();
            services.AddTransient<WebSocketEndpoint>();

            return services;
        }

        public static IEndpointConventionBuilder MapWebSocketManager(this IEndpointRouteBuilder endpoints, string pattern, Action<WebSocketSubProtocolBuilder> configureSubProtocols)
        {
            var subProtocolBuilder = new WebSocketSubProtocolBuilder(endpoints.ServiceProvider);
            configureSubProtocols?.Invoke(subProtocolBuilder);
            var subProtocolProvider = subProtocolBuilder.Build();

            return endpoints.Map(pattern, async context =>
            {
                var endpoint = context.RequestServices.GetRequiredService<WebSocketEndpoint>();

                await endpoint.HandleRequestAsync(context, subProtocolProvider);
            });
        }
    }
}
