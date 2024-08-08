using KittyBot.callbacks;
using KittyBot.database;
using KittyBot.services;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public class SetRussian: CallbackAction
{
    private readonly LocaleService _localeService;
    
    public SetRussian(LocaleService localeService)
    {
        _localeService = localeService;
    }

    public void Handle(CallbackQuery callback)
    {
        throw new NotImplementedException();
    }
}