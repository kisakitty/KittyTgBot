using Telegram.Bot.Types;

namespace KittyBot.callbacks;

public interface CallbackAction
{
    public void Handle(CallbackQuery callback);
}