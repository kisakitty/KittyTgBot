using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.services;

namespace KittyBot.buttons;

public class CallbackActionFactory
{
    private readonly Dictionary<string, CallbackAction> _callbackActions;

    public CallbackActionFactory(LocaleService localeService, ResponseConfigService responseConfigService)
    {
        _callbackActions = new Dictionary<string, CallbackAction>
        {
            { "englishLanguage", new SetEnglish(localeService)},
            { "russianLanguage", new SetRussian(localeService)},
            { "removeMessage", new RemoveMessage()},
        };
        foreach (ChatMode mode in Enum.GetValues(typeof(ChatMode)))
        {
            _callbackActions.Add(mode.ToString(), new SetChatMode(responseConfigService));
        }
    }

    public CallbackAction? GetCallbackActionByName(string name)
    {
        return !_callbackActions.TryGetValue(name, out var resultCommand) ? null : resultCommand;
    }
}