using KittyBot.exceptions;
using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using OxyPlot;
using OxyPlot.Core.Drawing;
using OxyPlot.Series;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KittyBot.handlers.commands;

public class GetStatsPieCommand: Command
{
    private readonly IServiceScopeFactory _scopeFactory;

    private const int Max1DResolution = 2500;
    private const int MaxFontSize = 100;
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;
    private const int DefaultFontSize = 28;
    private const int MinCountMessages = 100;

    public GetStatsPieCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task HandleCommand(ITelegramBotClient client, Message message, CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        var text = message.Text ?? "";
        var argv = ParseCommand(text);
        var widthString = argv.Length >= 2 ? argv[1] : null;
        var heightString = argv.Length >= 3 ? argv[2] : null;
        var fontSizeString = argv.Length >= 4 ? argv[3] : null;
        try
        {
            var width = widthString is null ? DefaultWidth : int.Parse(widthString);
            var height = heightString is null ? DefaultHeight : int.Parse(heightString);
            var fontSize = fontSizeString is null ? DefaultFontSize : float.Parse(fontSizeString);
            if (width > Max1DResolution || height > Max1DResolution || fontSize > MaxFontSize)
            {
                throw new BadInputException();
            }
            var pngExporter = new PngExporter
            {
                Width = width,
                Height = height
            };
            var stream = new MemoryStream();
            var model = BuildPlotModel(chatId, fontSize);
            pngExporter.Export(model, stream);
            await client.SendPhoto(
                chatId: chatId,
                photo: InputFile.FromStream(new MemoryStream(stream.ToArray())),
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
                cancellationToken: cancelToken);
        }
        catch (BadInputException)
        {
            await client.SendMessage(
                chatId: chatId,
                text: $"Слишком большое разрешение и/или размер шрифта. Попробуй до {Max1DResolution}x{Max1DResolution}. Максимальный шрифт — {MaxFontSize}",
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
                cancellationToken: cancelToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Birthday parse error. String: {message.Text}");
            await client.SendMessage(
                chatId: chatId,
                text: "Не могу распарсить комманду\\. Пришли в формате `/statschart <width> <height> <fontSize>`\\. \\<width\\>, \\<height\\> и \\<fontSize\\> опциональны \\(разрешение по умолчанию: 1920x1080\\, размер шрифта 28\\)\n\n" +
                      "" +
                      $"Также на графике не отображены участники, менее чем со {MinCountMessages} сообщениями",
                cancellationToken: cancelToken,
                replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
                parseMode: ParseMode.MarkdownV2);
        }
    }
    
    
    private IPlotModel BuildPlotModel(long chatId, float fontSize)
    {
        using var statsServiceScope = _scopeFactory.CreateScope();
        var statsService = statsServiceScope.ServiceProvider.GetRequiredService<StatsService>();

        var stats = statsService.GetGlobalStatsLinks(chatId, true).Where(statsItem => statsItem.Value > MinCountMessages);
        var slices = stats.Select(statsItem => new PieSlice(statsItem.Key, statsItem.Value) { Fill = RandomColor() } ).ToList();
        
        var model = new PlotModel { Title = "Статистика сообщений", TitleFontSize = fontSize * 1.3};

        var pieSeries = new PieSeries
        {
            Slices = slices,
            FontSize = fontSize
        };

        model.Series.Add(pieSeries);
        
        return model;
    }
    
    private static OxyColor RandomColor()
    {
        var r = new Random();
        return OxyColor.FromRgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
    }
}