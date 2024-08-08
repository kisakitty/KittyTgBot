using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.handlers;
using KittyBot.handlers.commands;
using KittyBot.services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot;

public class KittyBotService : IHostedService
{
    private const string TelegramEnv = "TELEGRAM_BOT_TOKEN";

    private const string AdminsListIdsEnv = "ADMIN_TG_IDS";

    private const string ChatsWhitelistEnv = "CHATS_WHITELIST";

    private readonly TelegramBotClient _botClient;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<long>? _allowedChats;

    private long? _myId;

    private string? _botname;

    public KittyBotService(IServiceScopeFactory scopeFactory, TelegramBotClient client)
    {
        string? chatsWhitelist = Environment.GetEnvironmentVariable(ChatsWhitelistEnv);
        Log.Information($"Allowed chats: {chatsWhitelist ?? "All"}");
        _allowedChats = chatsWhitelist != null ? chatsWhitelist.Split(",").Select(long.Parse).ToList() : null;

        _scopeFactory = scopeFactory;
        using (var scope = scopeFactory.CreateScope())
        {
            var kittyBotDbContext = scope.ServiceProvider.GetRequiredService<KittyBotContext>();
            kittyBotDbContext.Database.Migrate();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            string? adminIdList = Environment.GetEnvironmentVariable(AdminsListIdsEnv);
            if (adminIdList is not null)
            {
                userService.InitAdmins(adminIdList.Split(",").Select(long.Parse).ToList());
            }
        }

        string? token = Environment.GetEnvironmentVariable(TelegramEnv);
        if (token == null)
        {
            throw new EnvVariablesException($"Expect Telegram token. Set it to environment variable {TelegramEnv}");
        }

        _botClient = client;
        client.GetMeAsync().ContinueWith(Task =>
        {
            if (Task.Exception != null)
            {
                Log.Error(Task.Exception, "Unable to get bot's telegram ID");
                return;
            }

            _myId = Task.Result.Id;
            _botname = Task.Result.Username;
            Log.Information($"My bot ID: {_myId}");
            Log.Information($"My username: {_botname}");
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource cts = new();

        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            DropPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        return Task.CompletedTask;

        Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancelToken)
        {
            string errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Log.Error(exception, errorMessage);
            return Task.CompletedTask;
        }

        async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancelToken)
        {
            try
            {
                await HandleUpdateWithExceptions(client, update, cancelToken);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Something went wrong");
            }
        }
    }

    private async Task HandleUpdateWithExceptions(ITelegramBotClient client, Update update, CancellationToken cancelToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        var statsService = scope.ServiceProvider.GetRequiredService<StatsSerivce>();
        if (update.Message?.From is not null)
        {
            userService.CreateOrUpdateUser(update.Message.From.Id, update.Message.From.Username,
                update.Message.From.FirstName, update.Message.From.LastName);
        }

        if (update.CallbackQuery is { } callback)
        {
            HandleCallback(client, callback);
            return;
        }

        if (update.Message is not { } message)
        {
            return;
        }

        if (message.From != null)
        {
            statsService.LogStats(message.From, message.Chat.Id);
        }

        if (message.Type == MessageType.ChatMembersAdded)
        {
            var helloHandler = new HelloHandler();
            helloHandler.HandleUpdate(client, update, cancelToken);
            return;
        }

        // Only process text messages
        var messageText = message.Text ?? message.Caption;
        if (messageText == null)
        {
            return;
        }
        
        var reactionHandler = scope.ServiceProvider.GetRequiredService<ReactionHandler>();
        await reactionHandler.HandleUpdate(client, update, cancelToken);

        if (Util.IsCommand(messageText))
        {
            Log.Information(
                $"Command {messageText} in chat {update.Message.Chat.Id} from user {update.Message?.From?.Username ?? update.Message?.From?.FirstName}");
            HandleCommand(messageText, client, update, cancelToken);
            return;
        }
        
        // Chat whitelist
        if (_allowedChats != null && !_allowedChats.Contains(message.Chat.Id))
        {
            Log.Warning($"Allowed chats: {String.Join(", ", _allowedChats.Select(x => x.ToString()))}");
            Log.Warning(
                $"Skipped chat: {message.Chat.Id} | Title: {message.Chat.Title}");
            return;
        }

        ChatAiResponse(client, update, messageText, message, cancelToken);
        return;
    }

    private void ChatAiResponse(ITelegramBotClient client, Update update, string messageText,
        Message message, CancellationToken cancelToken)
    {
        var lowerMessage = messageText.ToLower();
        var replyToAuthor = message.ReplyToMessage?.From?.Username;
        if (lowerMessage.StartsWith("бот ") || lowerMessage.StartsWith("бот,") || lowerMessage.StartsWith("бот.") ||
            lowerMessage.Equals("бот") || lowerMessage.StartsWith($"@{_botname}") ||
            (replyToAuthor != null && replyToAuthor.Equals(_botname)))
        {
            using var scope = _scopeFactory.CreateScope();
            var geminiHandler = scope.ServiceProvider.GetRequiredService<GeminiHandler>();
            geminiHandler.GenerateResponse(client, update, _myId, cancelToken);
        }
    }

    private async Task HandleCommand(string command, ITelegramBotClient client, Update update,
        CancellationToken cancelToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<CommandFactory>();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        string commandName = command.Split()[0];
        try
        {
            if (update.Message?.From is not null && userService.IsAdmin(update.Message.From.Id))
            {
                var adminCommand = factory.GetAdminCommand(commandName, scope, _botname);
                if (adminCommand is not null)
                {
                    await adminCommand.HandleUpdate(client, update, cancelToken);
                }
            }
            else
            {
                var userCommand = factory.GetUserCommandByName(commandName, scope, _botname);
                if (userCommand is not null)
                {
                    await userCommand.HandleUpdate(client, update, cancelToken);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on handle command");
        }
    }

    private void HandleCallback(ITelegramBotClient client, CallbackQuery callback)
    {
        if (callback.Data is null || callback.Message is null) return;
        using var scope = _scopeFactory.CreateScope();
        var callbackFactory = scope.ServiceProvider.GetRequiredService<CallbackActionFactory>();
        var callbackAction = callbackFactory.GetCallbackActionByName(callback.Data);
        callbackAction?.Handle(callback);
        client.MakeRequestAsync(new AnswerCallbackQueryRequest(callback.Id));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}