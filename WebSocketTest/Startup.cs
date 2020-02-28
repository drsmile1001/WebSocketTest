using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace WebSocketTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseWebSockets(new WebSocketOptions 
            {
                ReceiveBufferSize = 4 * 1024
            });

            app.Use(async (context, next) =>
            {
                // 如果Request為WebSocket的
                if (context.WebSockets.IsWebSocketRequest)
                {
                    // 容許WebSocket連線並取得WebSocket實例
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    while (webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult receivedData = null;
                        // 接收一次訊息中的所有段落
                        do
                        {
                            // 接收緩衝區
                            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4 * 1024]);

                            // 接收
                            receivedData = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                            // 回傳
                            await webSocket.SendAsync(
                                buffer.Take(receivedData.Count).ToArray(),
                                receivedData.MessageType,
                                receivedData.EndOfMessage,
                                CancellationToken.None);
                        } while (!receivedData.EndOfMessage); // 是否為最後一的段落
                    }
                }
                else
                {
                    await next();
                }

            });
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Websocket Test");
            });
        }
    }
}
