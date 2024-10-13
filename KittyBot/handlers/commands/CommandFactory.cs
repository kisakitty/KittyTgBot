using Microsoft.Extensions.DependencyInjection;

namespace KittyBot.handlers.commands;

public class CommandFactory
{
    private readonly Dictionary<string, Command> _userCommands;
    private readonly Dictionary<string, Command> _adminCommands;

    private readonly Command _defaultCommand;
    
    private readonly IServiceScopeFactory _scopeFactory;

    public CommandFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _userCommands = new Dictionary<string, Command>
        {
            { "/start", new StartCommand() },
            { "/help", new HelpCommand() },
            { "/cat", new CatCommand() },
        };
        _adminCommands = new Dictionary<string, Command>();
        _defaultCommand = new UnknownCommand();
    }

    public Command? GetUserCommandByName(string commandName, IServiceScope scope, string? botname)
    {
        if (botname is not null && commandName.EndsWith($"@{botname}"))
        {
            commandName = commandName.Remove(commandName.Length - botname.Length - 1);
        }
        switch (commandName)
        {
            case "/stats": return scope.ServiceProvider.GetRequiredService<GetStatsCommand>();
            case "/piestats": return scope.ServiceProvider.GetRequiredService<GetStatsPieCommand>();
            case "/chartstats": return scope.ServiceProvider.GetRequiredService<GetStatsChartCommand>();
            case "/analytics": return scope.ServiceProvider.GetRequiredService<GetAnalyticsCommand>();
            case "/setbirthday": return scope.ServiceProvider.GetRequiredService<SetBirthdayCommand>();
            case "/removebirthday": return scope.ServiceProvider.GetRequiredService<RemoveBirthday>();
            case "/getbirthdays": return scope.ServiceProvider.GetRequiredService<GetBirthdaysCommand>();
            case "/hellomsg": return scope.ServiceProvider.GetRequiredService<ReverseHelloMessageConfigCommand>();
            case "/chatbot": return scope.ServiceProvider.GetRequiredService<ReverseChatBotCommand>();
            case "/clearcontext": return scope.ServiceProvider.GetRequiredService<ClearContextCommand>();
            case "/setmode": return scope.ServiceProvider.GetRequiredService<SetModeCommand>();
        }
        return _userCommands.GetValueOrDefault(commandName, _defaultCommand);
    }

    private Command? GetAdminCommandByName(string commandName, IServiceScope scope, string? botname)
    {
        if (botname is not null && commandName.EndsWith($"@{botname}"))
        {
            commandName = commandName.Remove(commandName.Length - botname.Length - 1);
        }
        return commandName switch
        {
            "/setbd" => scope.ServiceProvider.GetRequiredService<ForceSetBirthdayCommand>(),
            _ => _adminCommands.TryGetValue(commandName, out var resultCommand) ? null : resultCommand
        };
    }

    public Command? GetAdminCommand(string commandName, IServiceScope scope, string? botname)
    {
        return GetAdminCommandByName(commandName, scope, botname) ?? GetUserCommandByName(commandName, scope, botname);
    }
}