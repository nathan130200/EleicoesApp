using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace EleicoesBot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .MinimumLevel.Debug()
            .CreateLogger();

        var bot = new DiscordClient(new()
        {
            Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
            TokenType = TokenType.Bot,
            AlwaysCacheMembers = true,
            ReconnectIndefinitely = true,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Debug,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages
        });

        var cnext = bot.UseCommandsNext(new()
        {
            StringPrefixes = new[] { ";" },
            Services = new ServiceCollection()
                .AddLogging(x =>
                {
                    x.ClearProviders();
                    x.AddSerilog();
                })
                .AddSingleton<IApuracoesService, ApuracoesService>()
                .BuildServiceProvider(true)
        });

        cnext.RegisterCommands(typeof(Program).Assembly);

        await bot.ConnectAsync(new DiscordActivity()
        {
            ActivityType = ActivityType.Watching,
            Name = "ELEIÇÕES 2022"
        });

        await Task.Delay(-1);
    }
}