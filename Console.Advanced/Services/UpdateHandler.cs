using Console.Advanced.Data;
using Console.Advanced.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    async Task SaveContact(Message message)
    {
        long userId = message.Contact!.UserId!.Value;
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Include(x => x.Vacancy).ThenInclude(x => x.Position).FirstOrDefaultAsync(x => x.Id == userId);

        if(user is null)
        {
            await AskLanguage(message.Chat);
            return;
        }

        var hrJson = context.Settings.Where(x => x.Name == "Hr").First();
        User hr = JsonSerializer.Deserialize<User>(hrJson.Value)!;

        user.PhoneNumber = message.Contact.PhoneNumber.StartsWith('+') ?
                        message.Contact.PhoneNumber
                            : $"+{message.Contact.PhoneNumber}";

        var appFill = await context.AppFIlls.Where(x => x.Id == userId).FirstOrDefaultAsync();
        if(appFill is not null) {
            context.AppFIlls.Remove(appFill);
            await context.SaveChangesAsync();
        }

        if(hr.Id == message.From!.Id)
        {
            var oldContact = await context.Settings.Where(x => x.Name == "HrContact").FirstOrDefaultAsync();
            if(oldContact is not null) 
            {
                oldContact.Value = JsonSerializer.Serialize(message.Contact);
            }
            else
            {
                Settings HrContact = new()
                {
                    Name = "HrContact",
                    Value = JsonSerializer.Serialize(message.Contact),
                };
                context.Settings.Add(HrContact);
            }
            await context.SaveChangesAsync();
            await _bot.SendTextMessageAsync(message.Chat, 
                $"Ваш контакт сохранен для передачи соискателям",
                replyMarkup: new ReplyKeyboardRemove());
            await Usage(userId, message.Chat);
            return;
        }
        
        await context.SaveChangesAsync();

        string text = user.Language == Language.KY ?
            "Кабыл алынды 😊 HR-менеджер жакын арада сиз менен байланышат"
            : "Принято 😊 HR-менеджер свяжется с вами в ближайшее время";

        await _bot.SendTextMessageAsync(message.Chat, 
            text, 
            replyMarkup: new ReplyKeyboardRemove());

        await Usage(userId, message.Chat);

        var hrChatJson = context.Settings.Where(x => x.Name == "HrChat").First();
        Chat hrChat = JsonSerializer.Deserialize<Chat>(hrChatJson.Value)!;

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        int age = today.Year - user.DateOfBirth!.Value.Year;

        // Check if the birthday has occurred yet this year
        if (today < user.DateOfBirth!.Value.AddYears(age))
        {
            age--;
        }

        text = $@"Получен новый отклик на позицию {user.Vacancy.Position.RuName}! 
ФИО: {user.FullName} 
Возраст: {age} 
Тел. номер: {user.PhoneNumber}";

        await _bot.SendTextMessageAsync(hrChat, text);
    }

    private async Task OnMessage(Message message)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);

        if (message.Contact is not null)
        {
            await SaveContact(message);
            return;
        }

        if (message.Text is not { } messageText)
            return;
        long userId = message.From!.Id;
        Chat chat = message.Chat;

        Message sentMessage = await (messageText.Split(' ')[0] switch
        {
            "/vacancies" => ShowPositions(userId, chat),
            "/language" => AskLanguage(chat),
            "/city" => AskCity(userId, chat),
            "/application" => Application(userId, chat),
            "authorizeHr" => AuthorizeHr(message),
            _ => Application(userId, chat, message)
        });
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    async Task<Message> AuthorizeHr(Message message)
    {
        using var context = _contextFactory.CreateDbContext();
        var hrPassword = await context.Settings.FirstAsync(x => x.Name == "HrPassword");

        if (message.Text!.Split(' ')[1] != hrPassword.Value)
            return await _bot.SendTextMessageAsync(message.Chat, "Неверный пароль");

        Settings? hr = await context.Settings.Where(x => x.Name == "Hr").FirstOrDefaultAsync();

        if(hr is not null)
        {
            hr.Value = JsonSerializer.Serialize(message.From!);
        }
        else
        {
            hr = new Settings()
            {
                Name = "Hr",
                Value = JsonSerializer.Serialize(message.From!)
            };
            context.Settings.Add(hr);
        }

        Settings? hrChat = await context.Settings.Where(x => x.Name == "HrChat").FirstOrDefaultAsync();

        if(hrChat is not null)
        {
            hrChat.Value = JsonSerializer.Serialize(message.Chat!);
        }
        else
        {
            hrChat = new()
            {
                Name = "HrChat",
                Value = JsonSerializer.Serialize(message.Chat!)
            };
            context.Settings.Add(hrChat);
        }
        

        await context.SaveChangesAsync();


        return await _bot.SendTextMessageAsync(message.Chat, "Поздравляю, Азизжан доверяет вам управление наймом в Додо! Теперь все отклики на вакансии будут перенаправляться вам");
    }

    async Task<Message> Application(long userId, Chat chat, Message? message = null)
    {
        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return await AskLanguage(chat);

        AppFIll? appFill = await context.AppFIlls.Where(x => x.Id == userId).FirstOrDefaultAsync();
        
        if (appFill is null && message is null) 
        {
            appFill = new()
            {
                Id = userId,
                Stage = "Name"
            };
            context.AppFIlls.Add(appFill);
            await context.SaveChangesAsync();
            return await _bot.SendTextMessageAsync(chat, "Напишите свое ФИО");
        }

        if (appFill is null)
        {
            return await Usage(userId, chat);
        }

        if (appFill.Stage == "Name")
        {

            if (message.Text.Split(' ').Length < 2)
            {
                var txt = user.Language == Language.KY ?
                            "Аты-жөнүңүздү жазыңыз" :
                            "Напишите свое имя";

                return await _bot.SendTextMessageAsync(chat, txt);
            }
            appFill.Value = message.Text;
            await context.SaveChangesAsync();

            var rm = new InlineKeyboardMarkup()
                .AddNewRow().AddButton("Да", "application_Yes").AddButton("Нет", "application_No");

            var text = user.Language == Language.KY ?
                            $"Атыңыз {appFill.Value}?" :
                            $"Вас зовут {appFill.Value}?";

            return await _bot.SendTextMessageAsync(chat, text, replyMarkup: rm);
        }
        if(appFill.Stage == "Age")
        {
            try
            {
                DateOnly dob = DateOnly.ParseExact(message.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture);

                appFill.Value = message.Text;
                await context.SaveChangesAsync();

                var rm = new InlineKeyboardMarkup()
                .AddNewRow().AddButton("Да", "application_Yes").AddButton("Нет", "application_No");

                var text = user.Language == Language.KY ?
                            $"Туулган күнүңүз {appFill.Value}?" :
                            $"Ваша дата рождения {appFill.Value}?";

                return await _bot.SendTextMessageAsync(chat, text, replyMarkup: rm);
            }
            catch (Exception e)
            {
                return await AskDateOfBirth(userId, chat);
            }

        }
        return await AskLanguage(chat);
    }

    async Task<Message> AskDateOfBirth(long userId, Chat chat)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Where(x => x.Id == userId).FirstAsync();

        string text = user.Language == Language.KY ?
                        "Сураныч, туулган күнүңүздү кк.аа.жжжж форматында жазыңыз \nМисалы: 16.08.2000"
                        : "Пожалуйста, укажите свою дату рождения в формате дд.мм.гггг. \n Например: 16.08.2000";
        return  await _bot.SendTextMessageAsync(chat, text);
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

        string text = user.Language == Language.KY ?
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

    async Task<Message> Usage(long userId, Chat chat)
    {
        string usage = string.Empty;

        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return await AskLanguage(chat);

        string text = string.Empty;
        InlineKeyboardMarkup inlineMarkup = new();

        if(user.Language == Language.KY)
        {
            text = "Башкы меню";
            inlineMarkup.AddNewRow()
                    .AddButton("Тил тандоо", "language")
                .AddNewRow()
                    .AddButton("Шаарды тандоо", "city")
                .AddNewRow()
                    .AddButton("Бош орундар", "vacancies")
                .AddNewRow()
                    .AddButton("Анкета толтуруу", "application")
                .AddNewRow()
                    .AddButton("Менеджердин контакты", "hrContact");
        }
        else
        {
            text = "Главное меню";
            inlineMarkup.AddNewRow()
                    .AddButton("Выбор языка", "language")
                .AddNewRow()
                    .AddButton("Выбор города", "city")
                .AddNewRow()
                    .AddButton("Вакансии", "vacancies")
                .AddNewRow()
                    .AddButton("Заполнить анкету", "application")
                .AddNewRow()
                    .AddButton("Контакт менеджера", "hrContact");
        }

        return await _bot.SendTextMessageAsync(chat, text, replyMarkup: inlineMarkup);
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

        string callbackData = callbackQuery.Data!;
        Chat chat = callbackQuery.Message!.Chat;
        long userId = callbackQuery.From.Id;

        if (callbackData.StartsWith("position"))
        {
            int positionId = int.Parse(callbackData.Split('_')[1]);
            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
            await ShowVacancies(chat, userId, positionId);
            return;
        }

        if (callbackData.StartsWith("applyToVacancy"))
        {
            int vacancyId = int.Parse(callbackData.Split('_')[1]);
            await _bot.AnswerCallbackQueryAsync(callbackQuery!.Id);
            await ApplyToVacancy(chat, userId, vacancyId);
            return;
        }

        if (callbackData.StartsWith("application") && callbackData.Split('_').Length > 1)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQuery!.Id);

            using var context = _contextFactory.CreateDbContext();
            var appFill = await context.AppFIlls.Where(x => x.Id == userId).FirstAsync();

            string answer = callbackData.Split("_")[1];
            if (answer == "Yes")
            {
                var user = await context.Users.Where(x => x.Id == userId).FirstAsync();

                switch (appFill.Stage)
                {
                    case "Name":
                        user.FullName = appFill.Value;
                        appFill.Stage = "Age";
                        appFill.Value = null;
                        await context.SaveChangesAsync();
                        await AskDateOfBirth(userId, chat);
                        break;
                    case "Age":
                        user.DateOfBirth = DateOnly.ParseExact(appFill.Value!, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                        appFill.Stage = "Phone";
                        appFill.Value = null;
                        await context.SaveChangesAsync();
                        await AskPhone(userId, chat);
                        break;
                    default:
                        break;
                }
                return;
            }
            if(answer == "No")
            {
                switch(appFill.Stage)
                {
                    case "Age":
                        await AskDateOfBirth(userId, chat);
                        break;
                    case "Name":
                        await _bot.SendTextMessageAsync(chat, "Напишите свое ФИО");
                        break;
                    default:
                        break;
                }
            }
            return;
        }

        switch (callbackData)
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
                await AskLanguage(chat);
                break;
            case "city":
                await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                await AskCity(userId, chat);
                break;
            case "application":
                await _bot.AnswerCallbackQueryAsync(callbackQuery?.Id);
                await Application(callbackQuery.From.Id, callbackQuery.Message.Chat);
                break;
            case "vacancies":
                await _bot.AnswerCallbackQueryAsync(callbackQuery?.Id);
                await ShowPositions(callbackQuery.From.Id, callbackQuery.Message.Chat);
                break;
            case "usage":
                await _bot.AnswerCallbackQueryAsync(callbackQuery?.Id);
                await Usage(userId, chat);
                break;
            case "hrContact":
                await _bot.AnswerCallbackQueryAsync(callbackQuery.Id);
                await ShowHrContact(chat);
                break;
            default:
                throw new NotImplementedException($"Unknown callbackQuery: {callbackQuery.Data}");
        }

        //await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"Received {callbackQuery.Data}");
        //await _bot.SendTextMessageAsync(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
    }

    async Task<Message> AskPhone(long userId, Chat chat)
    {
        string keyText = string.Empty;
        string messageText = string.Empty;

        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Where(x => x.Id == userId).FirstAsync();

        if (user.Language == Language.RU)
        {
            messageText = "Пожалуйста, разрешите доступ к своему номеру, что бы менеджер мог с вами связаться";
            keyText = "Отправить свой тел. номер";
        }
        else
        {
            messageText = "Менеджер сиз менен байланышышы үчүн номериңизге кирүүгө уруксат бериңиз";
            keyText = "Телефон номеримди жөнөтүү";
        }

        var keyboard = new ReplyKeyboardMarkup().AddButton(KeyboardButton.WithRequestContact(keyText));

        return await _bot.SendTextMessageAsync(chat,
            messageText,
            replyMarkup: keyboard);
    }

    async Task ShowHrContact(Chat chat)
    {
        var context = _contextFactory.CreateDbContext();
        var json = await context.Settings.Where(x => x.Name == "HrContact").FirstAsync();
        Contact contact = JsonSerializer.Deserialize<Contact>(json.Value)!;

        await _bot.SendContactAsync(chat, contact.PhoneNumber, contact.FirstName, lastName: "Dodo HR");
    }

    async Task ApplyToVacancy(Chat chat, long userId, int vacancyId)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        
        user.Vacancy = await context.Vacancies.Where(x => x.Id == vacancyId).FirstAsync();
        await context.SaveChangesAsync();

        string keyText = string.Empty;
        string messageText = string.Empty;

        if (string.IsNullOrEmpty(user.FullName) || user.DateOfBirth is null)
        {
            await Application(userId, chat);
            return;
        }

        if (user.Language == Language.RU)
        {
            messageText = "Пожалуйста, разрешите доступ к своему номеру, что бы менеджер мог с вами связаться";
            keyText = "Отправить свой тел. номер";
        }
        else
        {
            messageText = "Менеджер сиз менен байланышышы үчүн номериңизге кирүүгө уруксат бериңиз";
            keyText = "Телефон номеримди жөнөтүү";
        }

        var keyboard = new ReplyKeyboardMarkup().AddButton(KeyboardButton.WithRequestContact(keyText));

        await _bot.SendTextMessageAsync(chat,
            messageText,
            replyMarkup: keyboard);
    }

    async Task<Message> ShowPositions(long userId, Chat chat)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.Include(x => x.City).FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null || string.IsNullOrEmpty(user.Language) || user.City is null)
            return await AskCity(userId, chat);

        var positions = await context.Positions.ToListAsync();
        InlineKeyboardMarkup inlineMarkup = new();
        string text = string.Empty;

        foreach (var position in positions)
        {
            text = user.Language == Language.KY ? position.KyName : position.RuName;

            inlineMarkup
                .AddNewRow().AddButton(text, $"position_{position.Id}");
        }

        text = user.Language == Language.KY ? "Бош орундар" : "Вакансии";
        return await _bot.SendTextMessageAsync(chat, text, replyMarkup: inlineMarkup);
    } 
    
    async Task<Message> ShowVacancies(Chat chat, long userId, int positionId)
    {
        using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FindAsync(userId);
        if (user is null || string.IsNullOrEmpty(user.Language))
            return await AskLanguage(chat);

        var vacancies = await context.Vacancies.Include(x => x.Position)
            .Where(x => x.Language == user.Language && x.Position.Id == positionId)
            .ToListAsync();

        foreach (var vacancy in vacancies)
        {
            await using var fileStream = new FileStream($"Files/{vacancy.Position.KyName}_{vacancy.Language}.jpg", FileMode.Open, FileAccess.Read);

            var replyMarkup = new InlineKeyboardMarkup();

            if (user.Language == Language.RU)
            {
                replyMarkup
                    .AddNewRow().AddButton("Откликнуться", $"applyToVacancy_{vacancy.Id}")
                    .AddNewRow().AddButton("Вакансии", "vacancies").AddButton("Главное меню", "usage");
            }
            else
            {
                replyMarkup
                    .AddNewRow().AddButton("Жооп берүү", $"applyToVacancy_{vacancy.Id}")
                    .AddNewRow().AddButton("Бош орундар", "vacancies").AddButton("Башкы меню", "usage");
            }
                

            await _bot.SendPhotoAsync(chat, fileStream, caption: vacancy.Text, replyMarkup: replyMarkup);
        }

        return await _bot.SendTextMessageAsync(chat, "");
    }

    async Task SetCity(CallbackQuery callbackQuery)
    {
        long userId = callbackQuery.From.Id;

        using var context = _contextFactory.CreateDbContext();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);


        if (user is null)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, "Тилди тандаңыз | Выберите язык");
            await AskLanguage(callbackQuery.Message!.Chat);
            return;
        }
       
        var city = await context.Cities.FirstAsync(x => x.Name == callbackQuery.Data!)
            ??throw new Exception($"A city with name {callbackQuery.Data} not found!");

        user.City = city;
        await context.SaveChangesAsync();
       
        string text = (user.Language == Language.RU) ?
            $"Выбран город {city.Name}" :
            $"{city.Name} шаары тандалды";

        await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, text);

        await Usage(userId, callbackQuery.Message!.Chat);
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
                Language = callbackQuery.Data!
            };
            context.Users.Add(user);
        }
        else
        {
            user.Language = callbackQuery.Data!;
        }

        await context.SaveChangesAsync();

        string text = (user.Language == Language.RU) ?
            "Выбран русский язык" :
            "Кыргыз тили тандалды";

        await _bot.AnswerCallbackQueryAsync(callbackQuery.Id, text);

        await Usage(userId, callbackQuery.Message!.Chat);
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
