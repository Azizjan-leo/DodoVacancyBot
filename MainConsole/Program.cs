using MainConsole;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


try
{
    CancellationTokenSource source = new();

    TelegramBotClient bot = new(BotSettings.Token, cancellationToken: source.Token);

    Dictionary<long, UserSettings> _usersSettings = new();

    const string lang_ru = "Русский";
    const string lang_ky = "Кыргызча";

    // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
    ReceiverOptions receiverOptions = new()
    {
        AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
    };

    async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var message = callbackQuery.Message;
        var settings = _usersSettings.GetValueOrDefault(userId);

        if (callbackQuery.Data == lang_ru || callbackQuery.Data == lang_ky)
        {
            string answerText = SetLang(userId, chatId, callbackQuery.Data, ref settings);

            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, answerText);

            await ShowMainMenu(userId, chatId, settings!);

            return;
        }
        else if(settings is null)
        {
            await AskLang(message);
            return;
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
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text;

        Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
        var settings = _usersSettings.GetValueOrDefault(message.From!.Id);

        if (settings is null)
        {
            await AskLang(message);
            return;
        }
        if (messageText == "/start")
        {
            await ShowMainMenu(userId, chatId, settings);
            return;
        }
        if(messageText == "/lang")
        {
            await AskLang(message);
            return;
        }
        if(messageText == "/vacancies" || 
            messageText == "Список вакансий" || 
            messageText == "Вакансиялардын тизмеси")
        {
            await GetVacancies(userId, chatId, settings);
            return;
        }
    }

    string SetLang(long userId, long chatId, string lang, ref UserSettings? settings)
    {       
        if (settings is not null)
            settings.Language = lang;
        else
        {
            settings = new() { Language = lang };

            _usersSettings.Add(userId, settings);
        }

        return lang == lang_ru ? "Русский язык установлен" : "Кыргыз тили орнотулду";
    }

    async Task AskLang(Message message)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        Message sentMessage = await bot.SendTextMessageAsync(
            chatId: chatId,
            text: "Выберите язык\n\nТилди тандаңыз",
            replyMarkup: langKeyboard,
            cancellationToken: source.Token);
    }

    async Task ShowMainMenu(long userId, long chatId, UserSettings settings)
    {
        List<List<KeyboardButton>> keys = settings.Language == lang_ky ?
        [
           ["Позиция тандоо", "Вакансиялардын тизмеси"]
        ]
        :
        [
          ["Выбор позиции", "Список вакансий"]
        ];

        string answerText = settings.Language == lang_ru ? "Главное меню" : "Башкы меню";

        await bot.SendTextMessageAsync(
            chatId,
            answerText,
            replyMarkup: new ReplyKeyboardMarkup(keys) { ResizeKeyboard = true });
    }

    async Task GetVacancies(long userId, long chatId, UserSettings settings)
    {
        string text = settings.Language == lang_ru ? "Типа вакансии" : "Вакансиялар тизмеси";

        Message sentMessage = await bot.SendTextMessageAsync(
           chatId: chatId,
           text: text,
           cancellationToken: source.Token);
    }

    async Task OnUnhandledUpdate(Update update) 
        => Console.WriteLine($"Received unhandled update {update.Type}");
    
}
catch (Exception ex)
{

	throw;
}