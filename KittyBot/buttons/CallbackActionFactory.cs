using KittyBot.buttons;
using KittyBot.services;

namespace KittyBot.callbacks;

public class CallbackActionFactory
{
    private readonly Dictionary<string, CallbackAction> _callbackActions;

    public CallbackActionFactory(LocaleService localeService)
    {
        _callbackActions = new Dictionary<string, CallbackAction>
        {
            { "englishLanguage", new SetEnglish(localeService)},
            { "russianLanguage", new SetRussian(localeService)}
        };
    }

    public CallbackAction? GetCallbackActionByName(string name)
    {
        return !_callbackActions.TryGetValue(name, out var resultCommand) ? null : resultCommand;
    }
}