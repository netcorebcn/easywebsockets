using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EasyWebSockets
{
    public static class WebSocketManagerExtensions
    {
        public static IServiceCollection AddEasyWebSockets(this IServiceCollection services)
        {
            services.AddTransient<WebSocketConnectionManager>();
            services.AddSingleton<IWebSocketPublisher, WebSocketHandler>();

            return services;
        }

        public static IApplicationBuilder UseEasyWebSockets(this IApplicationBuilder app, string path = "/ws")
        {
            app.UseWebSockets();

            object wsHandler = app.ApplicationServices.GetRequiredService<IWebSocketPublisher>();
            return app.Map(new PathString(path), builder => builder.UseMiddleware<WebSocketManagerMiddleware>(wsHandler));
        }
    }
}