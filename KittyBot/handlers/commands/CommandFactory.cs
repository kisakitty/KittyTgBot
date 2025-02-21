using Microsoft.Extensions.DependencyInjection;

namespace KittyBot.handlers.commands;

public class CommandFactory
{
    private readonly Dictionary<string, Command> _adminCommands = new();

    private readonly Command _defaultCommand = new UnknownCommand();

    private readonly Dictionary<string, Command> _userCommands = new()
    {
        { "/start", new StartCommand() },
        { "/help", new HelpCommand() },
        { "/cat", new CatCommand() }
    };

    public Command? GetUserCommandByName(string commandName, IServiceScope scope, string? botname)
    {
        if (botname is not null && commandName.EndsWith($"@{botname}"))
            commandName = commandName.Remove(commandName.Length - botname.Length - 1);
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
            case "/reacts": return scope.ServiceProvider.GetRequiredService<GetReactionsStatisticsCommand>();
        }

        return _userCommands.GetValueOrDefault(commandName, _defaultCommand);
    }

    private Command? GetAdminCommandByName(string commandName, IServiceScope scope, string? botname)
    {
        if (botname is not null && commandName.EndsWith($"@{botname}"))
            commandName = commandName.Remove(commandName.Length - botname.Length - 1);
        return commandName switch
        {
            "/setbd" => scope.ServiceProvider.GetRequiredService<ForceSetBirthdayCommand>(),
            "/invalidate" => scope.ServiceProvider.GetRequiredService<DeactivateInactiveChats>(),
            _ => _adminCommands.TryGetValue(commandName, out var resultCommand) ? null : resultCommand
        };
    }

    public Command? GetAdminCommand(string commandName, IServiceScope scope, string? botname)
    {
        return GetAdminCommandByName(commandName, scope, botname) ?? GetUserCommandByName(commandName, scope, botname);
    }
}