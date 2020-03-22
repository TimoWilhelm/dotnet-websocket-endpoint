using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tiwi.Sockets
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<ConnectionManager>();
            services.AddScoped<WebSocketEndpoint>();

            return services;
        }

        public static IEndpointConventionBuilder MapWebSocketManager<T>(this IEndpointRouteBuilder endpoints, string pattern) where T : WebSocketHandler
        {
            return endpoints.Map(pattern, async context =>
            {
                var webSocketEndpoint = context.RequestServices.GetRequiredService<WebSocketEndpoint>();
                var handler = context.RequestServices.GetRequiredService<T>();

                await webSocketEndpoint.HandleRequestAsync(context, handler);
            });
        }
    }
}