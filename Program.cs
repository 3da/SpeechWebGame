using System.Runtime.Versioning;
using SpeechWebGame.Games;
using SpeechWebGame.Tools;

namespace SpeechWebGame
{
    [SupportedOSPlatform("windows")]
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //builder.WebHost.UseUrls("http://*:5009", "https://*:5010");

            builder.WebHost.UseUrls("http://*:5108", "http://trees.adamenko.net");

            builder.WebHost.UseHttpSys();

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddSignalR(q =>
            {
                q.EnableDetailedErrors = true;
                q.MaximumParallelInvocationsPerClient = 4;

            }).AddMessagePackProtocol();
            builder.Services.AddSpaStaticFiles(q => q.RootPath = "wwwroot\\client-app");

            builder.Services.AddSingleton<AudioWorker>();
            builder.Services.AddSingleton<SpeechService>();
            builder.Services.AddSingleton<InputService>();
            builder.Services.AddSingleton<GameRunner>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();

            app.MapHub<MainHub>("/api/main", q => { });


            app.MapWhen(x => !x.Request.Path.Value.StartsWith("/api"), builder =>
            {
                builder.UseSpaStaticFiles();
                builder.UseSpa(q =>
                {
#if DEBUG
                    q.UseProxyToSpaDevelopmentServer("http://localhost:5173/");
#endif
                });
            });

            app.Run();
        }
    }
}
