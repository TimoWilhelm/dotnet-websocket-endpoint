using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tiwi.Sockets.Examples.Chat
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
            services.AddTransient<ChatMessageProtocolHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2),
                ReceiveBufferSize = 8192,
            };
            // webSocketOptions.AllowedOrigins.Add();

            app.UseWebSockets(webSocketOptions);

            app.UseEndpoints(endpoints => endpoints.MapWebSocketManager("/ws", o => o.AddSubProtocol<ChatMessageProtocolHandler>()));

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
