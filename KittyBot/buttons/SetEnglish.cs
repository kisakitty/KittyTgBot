using KittyBot.callbacks;
using KittyBot.services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.buttons;

public class SetEnglish: CallbackAction
{
    private readonly LocaleService _localeService;
    
    public SetEnglish(LocaleService localeService)
    {
        _localeService = localeService;
    }
    
    public void Handle(ITelegramBotClient client, CallbackQuery callback, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}