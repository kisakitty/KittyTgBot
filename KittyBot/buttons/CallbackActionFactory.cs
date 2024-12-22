using KittyBot.database;
using KittyBot.services;

namespace KittyBot.buttons;

public class CallbackActionFactory
{
    private readonly Dictionary<string, ICallbackAction> _callbackActions;

    public CallbackActionFactory(LocaleService localeService, ResponseConfigService responseConfigService)
    {
        _callbackActions = new Dictionary<string, ICallbackAction>
        {
            // { "englishLanguage", new SetEnglish(localeService) },
            // { "russianLanguage", new SetRussian(localeService) },
            { "removeMessage", new RemoveMessage() }
        };
        foreach (ChatMode mode in Enum.GetValues(typeof(ChatMode)))
            _callbackActions.Add(mode.ToString(), new SetChatMode(responseConfigService));
    }

    public ICallbackAction? GetCallbackActionByName(string name)
    {
        return _callbackActions.GetValueOrDefault(name);
    }
}