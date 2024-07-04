using MainConsole;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


try
{
    CancellationTokenSource source = new();

    TelegramBotClient bot = new(BotSettings.Token, cancellationToken: source.Token);

    const string lang_ru = "Русский";
    const string lang_ky = "Кыргызча";

    string? lang = null;

    // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
    ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };

    async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        if(callbackQuery.Data == lang_ru)
        {
            lang = lang_ru;

            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, callbackQuery.Data);

            List<List<KeyboardButton>> rusKeys =
          [
              ["Выбор позиции", "Список вакансий"]
          ];
            await bot.SendTextMessageAsync(
                callbackQuery.Message!.Chat.Id, 
                "Русский язык установлен",
                replyMarkup: new ReplyKeyboardMarkup(rusKeys) { ResizeKeyboard = true });
        }

        if(callbackQuery.Data == lang_ky)
        {
            lang = lang_ky;

            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, callbackQuery.Data);

            List<List<KeyboardButton>> rusKeys =
           [
               ["Позиция тандоо", "Вакансиялардын тизмеси"]
           ];
            await bot.SendTextMessageAsync(callbackQuery.Message!.Chat.Id, 
                "Кыргыз тили орнотулду",
                 replyMarkup: new ReplyKeyboardMarkup(rusKeys) { ResizeKeyboard = true });
        }    

    }

    var kyrBtn = InlineKeyboardButton.WithCallbackData(lang_ky);
    var rusBtn = InlineKeyboardButton.WithCallbackData(lang_ru);

    InlineKeyboardMarkup langKeyboard =
        new([kyrBtn, rusBtn]);
    
    bot.StartReceiving(OnUpdate, async (bot, ex, ct) => Console.WriteLine(ex));

    var me = await bot.GetMeAsync();

    Console.WriteLine($"Start listening for @{me.Username}");
    Console.ReadLine();

    // Send cancellation request to stop bot
    source.Cancel();

    async Task OnUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        await (update switch
        {
            { Message: { } message } => OnMessage(message),
           //{ EditedMessage: { } message } => OnMessage(message, true),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
            _ => OnUnhandledUpdate(update)
        });
    }

    async Task OnMessage(Message message)
    {
        var chatId = message.Chat.Id;
        var messageText = message.Text;

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

        if (messageText == "/start" || messageText == "/lang")
        {
            Message sentMessage = await bot.SendTextMessageAsync(
               chatId: chatId,
               text: "Выберите язык\n\nТилди тандаңыз",
               replyMarkup: langKeyboard,
               cancellationToken: source.Token);
        }
    }
    async Task OnUnhandledUpdate(Update update) => Console.WriteLine($"Received unhandled update {update.Type}");
    
}
catch (Exception ex)
{

	throw;
}