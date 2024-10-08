using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.handlers;
using KittyBot.handlers.commands;
using KittyBot.services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using Serilog;
using Telegram.Bot;

namespace KittyBot;
public static class Program
{
    private const string LogDirectoryEnv = "KITTY_LOG_DIRECTORY";
    
    private const string TelegramEnv = "TELEGRAM_BOT_TOKEN";

    private const string OpenAiTokenEnv = "OPENAI_TOKEN";

    public static void Main() => CreateHostBuilder().Build().Run();

    private static IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            SetLogger();
            services.AddDbContext<KittyBotContext>(options =>
            {
                options.UseNpgsql(KittyBotContext.GetConnectionString());
            });
            Log.Information("Application have been started");
            services.AddScoped<UserService>();
            services.AddScoped<EventService>();
            services.AddScoped<MessageService>();
            services.AddScoped<StatsSerivce>();
            services.AddScoped<ResponseConfigService>();
            services.AddScoped<AnalyticsService>();
            services.AddScoped<BirthdaysService>();
            services.AddScoped<LocaleService>();
            services.AddScoped<CommandFactory>();
            services.AddScoped<GetStatsCommand>();
            services.AddScoped<GetAnalyticsCommand>();
            services.AddScoped<GetBirthdaysCommand>();
            services.AddScoped<ClearContextCommand>();
            services.AddScoped<SetBirthdayCommand>();
            services.AddScoped<ForceSetBirthdayCommand>();
            services.AddScoped<GetStatsPieCommand>();
            services.AddScoped<GetStatsChartCommand>();
            services.AddScoped<ReverseHelloMessageConfigCommand>();
            services.AddScoped<ReverseChatBotCommand>();
            services.AddScoped<ReactionHandler>();
            services.AddScoped<RemoveBirthday>();
            SetTelegramClient(services);
            SetOpenAiClient(services);
            services.AddScoped<CallbackActionFactory>();
            services.AddScoped<OpenAiHandler>();
            // services.AddScoped<Gpt4FreeHandler>();
            services.AddScoped<GeminiHandler>();
            services.AddHostedService<KittyBotService>();
            services.AddHostedService<EventsNotifier>();
            services.AddHostedService<BirthdaysNotifier>();
        })
        .ConfigureLogging((context, logging) =>
        {
            var config = context.Configuration.GetSection("Logging");
            logging.AddConfiguration(config);
            logging.AddConsole();
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

        });

    private static void SetTelegramClient(IServiceCollection services)
    {
        string? token = Environment.GetEnvironmentVariable(TelegramEnv);
        if (token == null)
        {
            throw new EnvVariablesException($"Expect Telegram token. Set it to environment variable {TelegramEnv}");
        }
        services.AddSingleton(new TelegramBotClient(token));
    }

    private static void SetOpenAiClient(IServiceCollection services)
    {
        string? openAiToken = Environment.GetEnvironmentVariable(OpenAiTokenEnv);
        if (openAiToken == null)
        {
            throw new EnvVariablesException($"Expect Open AI token. Set it to environment variable {OpenAiTokenEnv}");
        }
        services.AddSingleton(new OpenAIClient(new OpenAIAuthentication(openAiToken)));
    }

    private static void SetLogger()
    {
        string? logDirectory = Environment.GetEnvironmentVariable(LogDirectoryEnv);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console();
        if (logDirectory != null)
        {
            loggerConfiguration = loggerConfiguration.WriteTo.File(logDirectory, rollingInterval: RollingInterval.Day);   
        }
        Log.Logger = loggerConfiguration.CreateLogger();   
    }
}
