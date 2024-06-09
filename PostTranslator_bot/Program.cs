using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;


var botClient = new TelegramBotClient("7000977988:AAFj4UO5JfSNdgA22rPboQ_S4GxVQqIZeB8");
var apiKey = "394c1f12d8msh29cdd2b2cf5289ep1fb008jsnbc113eb2a87a";
var apiHost = "microsoft-translator-text.p.rapidapi.com";

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new ()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;
    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    var translator = new Translator(apiKey,apiHost);

    try
    {
        string translatedText = await translator.TranslateText(messageText);
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Ваш перевод:\n" + translatedText,
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error during translation: " + ex.Message);
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Произошла ошибка при переводе текста. Пожалуйста, попробуйте позже.",
            cancellationToken: cancellationToken);
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}