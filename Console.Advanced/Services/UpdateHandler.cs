using Console.Advanced.Data;
using Console.Advanced.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Console.Advanced.Services;

public sealed class UpdateHandler(ILogger<UpdateHandler> _logger, ITelegramBotClient _bot, 
    MyDbContextFactory _contextFactory, IOptions<BotConfiguration> _botConfig) : IUpdateHandler
{
    private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];
    
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message }                        => OnMessage(message),
            { EditedMessage: { } message }                  => OnMessage(message),
            { CallbackQuery: { } callbackQuery }            => OnCallbackQuery(callbackQuery),
            { InlineQuery: { } inlineQuery }                => OnInlineQuery(inlineQuery),
            { ChosenInlineResult: { } chosenInlineResult }  => OnChosenInlineResult(chosenInlineResult),
            { Poll: { } poll }                              => OnPoll(poll),
            { PollAnswer: { } pollAnswer }                  => OnPollAnswer(pollAnswer),
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            _                                               => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        _logger.LogInformation("Receive message type: {MessageType}", msg.Type);
        if (msg.Text is not { } messageText)
            return;

        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/language" => AskLanguage(msg.Chat),
            "/city" => AskCity(msg.From.Id, msg.Chat),
            "/photo" => SendPhoto(msg),
            "/inline_buttons" => SendInlineKeyboard(msg),
            "/keyboard" => SendReplyKeyboard(msg),
            "/remove" => RemoveKeyboard(msg),
            "/request" => RequestContactAndLocation(msg),
            "/inline_mode" => StartInlineQuery(msg),
            "/poll" => SendPoll(msg),
            "/poll_anonymous" => SendAnonymousPoll(msg),
            "/throw" => FailingHandler(msg),
            _ => Usage(msg)
        });
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    async Task<Message> AskCity(long userId, Chat chat)
    {
        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FindAsync(userId);

        if (user is null)
            return await AskLanguage(chat);

        var replyMarkup = new InlineKeyboardMarkup();

        foreach (var city in await context.Cities.ToListAsync())
            replyMarkup.AddNewRow().AddButton(city.Name, city.Name);

        string text = user.Lang == Language.KY ?
            "Шаарды тандаңыз" : "Выберите город";

        return await _bot.SendTextMessageAsync(chat, text, replyMarkup: replyMarkup);
    }

    async Task<Message> AskLanguage(Chat chat)
    {
        var replyMarkup = new InlineKeyboardMarkup()
            .AddNewRow()
                .AddButton("Кыргыз тили", Language.KY)
                .AddButton("Русский", Language.RU);

        return await _bot.SendTextMessageAsync(chat, "Тилди тандаңыз\n\nВыберите язык", replyMarkup: replyMarkup);
    }

    async Task<Message> Usage(Message msg)
    {
        string usage = string.Empty;    

        long userId = msg.From!.Id;

        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return await AskLanguage(msg.Chat);

        InlineKeyboardMarkup inlineMarkup = new();

        if(user.Lang == Language.KY)
        {
            inlineMarkup.AddNewRow()
                    .AddButton("Тил тандоо", "language")
                .AddNewRow()
                    .AddButton("Шаарды тандоо", "city");
                //.AddNewRow()
                //    .AddButton("Бош орундар", "vacanies");
        }
        else
        {
            inlineMarkup.AddNewRow()
                    .AddButton("Выбор языка", "language")
                .AddNewRow()
                    .AddButton("Выбор города", "city");
                //.AddNewRow()
                //    .AddButton("Вакансии", "vacanies");
        }

        
        return await _bot.SendTextMessageAsync(msg.Chat, "Меню:", replyMarkup: inlineMarkup);
    }

    async Task<Message> SendPhoto(Message msg)
    {
        await _bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
        await Task.Delay(2000); // simulate a long task
        await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
        return await _bot.SendPhotoAsync(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
    }

    // Send inline keyboard. You can process responses in OnCallbackQuery handler
    async Task<Message> SendInlineKeyboard(Message msg)
    {
        var inlineMarkup = new InlineKeyboardMarkup()
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow()
                .AddButton("WithCallbackData", "CallbackData")
                .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));
        return await _bot.SendTextMessageAsync(msg.Chat, "Inline buttons:", replyMarkup: inlineMarkup);
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddNewRow("1.1", "1.2", "1.3")
            .AddNewRow().AddButton("2.1").AddButton("2.2");
        return await _bot.SendTextMessageAsync(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
    }

    async Task<Message> RemoveKeyboard(Message msg)
    {
        return await _bot.SendTextMessageAsync(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
    }

    async Task<Message> RequestContactAndLocation(Message msg)
    {
        var replyMarkup = new ReplyKeyboardMarkup(true)
            .AddButton(KeyboardButton.WithRequestLocation("Location"))
            .AddButton(KeyboardButton.WithRequestContact("Contact"));
        return await _bot.SendTextMessageAsync(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
    }

    async Task<Message> StartInlineQuery(Message msg)
    {
        var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
        return await _bot.SendTextMessageAsync(msg.Chat, "Press the button to start Inline Query\n\n" +
            "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
    }

    async Task<Message> SendPoll(Message msg)
    {
        return await _bot.SendPollAsync(msg.Chat, "Question", PollOptions, isAnonymous: false);
    }

    async Task<Message> SendAnonymousPoll(Message msg)
    {
        return await _bot.SendPollAsync(chatId: msg.Chat, "Question", PollOptions);
    }

    static Task<Message> FailingHandler(Message msg)
    {
        throw new NotImplementedException("FailingHandler");
    }

    // Process Inline Keyboard callback data
    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        switch (callbackQuery.Data)
        {
            case Language.KY:
                await SetLanguage(callbackQuery);
                break;
            case Language.RU:
                await SetLanguage(callbackQuery);
                break;
            case "Бишкек":
                await SetCity(callbackQuery);
                break;
            case "language":
                await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                await AskLanguage(callbackQuery.Message!.Chat);
                break;
            case "city":
                await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                await AskCity(callbackQuery.From.Id, callbackQuery.Message.Chat);
                break;
            default:
                throw new NotImplementedException($"Unknown callbackQuery: {callbackQuery.Data}");
        }

        //await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"Received {callbackQuery.Data}");
        //await _bot.SendTextMessageAsync(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
    }

    async Task SetCity(CallbackQuery callbackQuery)
    {
        long userId = callbackQuery.From.Id;

        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);


        if (user is null)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, "Тилди тандаңыз | Выберите язык");
            await AskLanguage(callbackQuery.Message.Chat);
            return;
        }
       
        var city = await context.Cities.FirstAsync(x => x.Name == callbackQuery.Data!)
            ??throw new Exception($"A city with name {callbackQuery.Data} not found!");

        user.City = city;
        await context.SaveChangesAsync();
       
        string text = (user.Lang == Language.RU) ?
            $"Выбран город {city.Name}" :
            $"{city.Name} шаары тандалды";

        Message msg = new()
        {
            Chat = callbackQuery.Message.Chat,
            From = new User { Id = userId },
        };

        await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, text);

        await Usage(msg);
    }

    async Task SetLanguage(CallbackQuery callbackQuery)
    {
        long userId = callbackQuery.From.Id;

        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        

        if (user is null)
        {
            user = new AppUser()
            {
                Id = userId,
                Lang = callbackQuery.Data!
            };
            context.Users.Add(user);
        }
        else
        {
            user.Lang = callbackQuery.Data!;
        }

        await context.SaveChangesAsync();

        string text = (user.Lang == Language.RU) ?
            "Выбран русский язык" :
            "Кыргыз тили тандалды";

        await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, text);

        Message msg = new()
        {
            From = new User() { Id = callbackQuery.From.Id },
            Chat = callbackQuery.Message.Chat
        };

        await Usage(msg);
    }

    async Task OnError(long chatId, string? language)
    {
        string text = (language == Language.RU) ?
            "Произошла неизвестная ошибка. Пожалуйста, повторите позже" :
            "Белгисиз ката болду. Кийинчерээк кайра аракет кылып көрүңүз";

        await _bot.SendTextMessageAsync(chatId, text);
    }

    #region Inline Mode

    private async Task OnInlineQuery(InlineQuery inlineQuery)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [ // displayed result
            new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
        ];
        await _bot.AnswerInlineQueryAsync(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
    }

    private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
        await _bot.SendTextMessageAsync(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
    }

    #endregion

    private Task OnPoll(Poll poll)
    {
        _logger.LogInformation("Received Poll info: {Question}", poll.Question);
        return Task.CompletedTask;
    }

    private async Task OnPollAnswer(PollAnswer pollAnswer)
    {
        var answer = pollAnswer.OptionIds.FirstOrDefault();
        var selectedOption = PollOptions[answer];
        if (pollAnswer.User != null)
            await _bot.SendTextMessageAsync(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}
