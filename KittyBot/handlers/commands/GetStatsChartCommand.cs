using KittyBot.services;
using Microsoft.Extensions.DependencyInjection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Core.Drawing;
using OxyPlot.Series;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KittyBot.handlers.commands;

public class GetStatsChartCommand(IServiceScopeFactory scopeFactory) : Command
{
    private const int MinCountMessages = 100;

    protected override async Task HandleCommand(ITelegramBotClient client, Message message,
        CancellationToken cancelToken)
    {
        var chatId = message.Chat.Id;
        var pngExporter = new PngExporter
        {
            Width = 1920,
            Height = 1080
        };
        var stream = new MemoryStream();
        var model = BuildPieModel(chatId);
        pngExporter.Export(model, stream);
        await client.SendPhoto(
            chatId,
            InputFile.FromStream(new MemoryStream(stream.ToArray())),
            replyParameters: new ReplyParameters { ChatId = message.Chat.Id, MessageId = message.MessageId },
            cancellationToken: cancelToken);
    }

    private IPlotModel BuildPieModel(long chatId)
    {
        using var statsServiceScope = scopeFactory.CreateScope();
        var statsService = statsServiceScope.ServiceProvider.GetRequiredService<StatsService>();

        var stats = statsService
            .GetGlobalStatsLinks(chatId, true, false).Where(statsItem => statsItem.Value > MinCountMessages)
            .ToList();
        var barItems = stats.Select((statsItem, index) => new BarItem
            { Value = statsItem.Value, CategoryIndex = index, Color = RandomColor() });
        var model = new PlotModel { Title = "Статистика сообщений", TitleFontSize = 36 };
        var barSeries = new BarSeries
        {
            ItemsSource = barItems,
            LabelPlacement = LabelPlacement.Base,
            LabelFormatString = "{0}",
            FontSize = 24
        };
        model.Series.Add(barSeries);

        model.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Left,
            Angle = 45,
            Key = "Участники",
            ItemsSource = stats.Select(statsItem => statsItem.Key),
            FontSize = 24
        });

        return model;
    }

    private static OxyColor RandomColor()
    {
        var r = new Random();
        return OxyColor.FromRgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
    }
}