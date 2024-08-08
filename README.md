# Telegram Chat Bot

It is just telegram chatbot which help user to get cat's images and talk with chat assistant

# Deploying

## Docker

```shell
docker start -e 'TELEGRAM_BOT_TOKEN=<your token>' ... <other options> ... kisakitty/kitty-bot
```

# Demo

You can try this bot on the [link](https://t.me/kisakittybot)

# Environment variables

| variable              | meaning                                                  | necessary | Default value            |
|-----------------------|----------------------------------------------------------|-----------|--------------------------|
| `TELEGRAM_BOT_TOKEN`  | Telegram bot token from @BotFather                       | true      |                          |
| `OPENAI_TOKEN`        | Open AI token                                            | true      |                          |
| `GOOGLE_API_KEY`      | Gemini API token                                         | true      |                          |
| `ADMIN_TG_IDS`        | Telegram user id list of admins                          | false     | null                     |
| `YT_PS_USERNAME`      | Postgres username to save statistic                      | false     | postgres                 |
| `YT_PS_PASSWORD`      | Postgres password to save statistic                      | false     | Empty password           |
| `YT_PS_HOSTNAME`      | Postgres hostname to save statistic                      | false     | localhost                |
| `YT_PS_DATABASE`      | Postgres database name to save statistic                 | false     | same as `YT_PS_USERNAME` |
| `KITTY_LOG_DIRECTORY` | path to log files                                        | false     | null                     |
| `CHATS_WHITELIST`     | Allowed telegram chats ids                               | false     | All chats                |

# Planned features

- The daily summary in chat
- Calculate daily statistics
