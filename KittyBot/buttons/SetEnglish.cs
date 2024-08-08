using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.services;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public class SetEnglish: CallbackAction
{
    private readonly LocaleService _localeService;
    
    public SetEnglish(LocaleService localeService)
    {
        _localeService = localeService;
    }
    
    public void Handle(CallbackQuery callback)
    {
        throw new NotImplementedException();
    }
}