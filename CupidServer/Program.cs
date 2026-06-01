using CupidServer.hubs;
using CupidServer.services;

namespace CupidServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();

            builder.Services.AddSingleton<CupidService>();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<CupidService>());

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<CupidHub>("/cupidHub");

            app.Run();
        }
    }
}