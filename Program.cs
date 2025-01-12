using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class EcoBot
{
    private static TelegramBotClient botClient;
    private static readonly Dictionary<long, QuizState> UserQuizzes = new();

    class QuizState
    {
        public int CurrentQuestionIndex { get; set; }
        public int Score { get; set; }
    }

    static async Task Main(string[] args)
    {
        botClient = new TelegramBotClient("7845096731:AAFcO61MY96qGMAz9PHZV7b8Z5f1zWM_HYo"); // Замените на свой токен

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Бот запущен!");
        Console.ReadLine();

        cts.Cancel();
    }

    private static string GetWelcomeMessage()
    {
        return "Привет! Я ЭкоБот. Вот что я умею:\n" +
               "1. Викторина\n" +
               "2. Экологические проблемы\n" +
               "3. Советы по экологии\n" +
               "4. Найти эко-пункты в вашем городе\n" +
               "Введите номер от 1 до 4, чтобы начать!";
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Ошибка Telegram API:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }


    private static async Task ShowMainMenu(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
        new KeyboardButton[] { "1", "2", "3", "4" }
    })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        string menuMessage = "Выберите действие:\n" +
                             "1. Викторина\n" +
                             "2. Экологические проблемы\n" +
                             "3. Советы по экологии\n" +
                             "4. Найти эко-пункты в вашем городе";

        await botClient.SendTextMessageAsync(chatId, menuMessage, replyMarkup: keyboard, cancellationToken: cancellationToken);
    }


    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            await HandleMessageAsync(update.Message, cancellationToken);
        }
    }

    private static async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        long chatId = message.Chat.Id;
        string userMessage = message.Text.ToLower();

        if (UserQuizzes.ContainsKey(chatId))
        {
            await HandleQuizResponse(chatId, userMessage, cancellationToken);
        }
        else
        {
            switch (userMessage)
            {
                case "/start":
                    await botClient.SendTextMessageAsync(chatId, GetWelcomeMessage(), cancellationToken: cancellationToken);
                    break;
                case "1":
                    await StartQuizAsync(chatId, cancellationToken);
                    break;
                case "2":
                    await InformAboutEcologicalProblemsAsync(chatId, cancellationToken);
                    break;
                case "3":
                    await GiveEcoTipsAsync(chatId, cancellationToken);
                    break;
                case "4":
                    await FindEcoPointsAsync(chatId, cancellationToken);
                    break;
                case "сургут":
                case "нижневартовск":
                case "ханты-мансийск":
                    await HandleCitySelection(chatId, userMessage, cancellationToken);
                    break;
                default:
                    await botClient.SendTextMessageAsync(chatId, "Извините, я не понимаю. Попробуйте что-то другое.", cancellationToken: cancellationToken);
                    await ShowMainMenu(chatId, cancellationToken);
                    break;
            }
        }
    }

    private static async Task StartQuizAsync(long chatId, CancellationToken cancellationToken)
    {
        var quizState = new QuizState
        {
            CurrentQuestionIndex = 0,
            Score = 0
        };
        UserQuizzes[chatId] = quizState;

        await SendQuizQuestion(chatId, quizState, cancellationToken);
    }

    private static async Task SendQuizQuestion(long chatId, QuizState quizState, CancellationToken cancellationToken)
    {
        string[] questions = {
            "1. Какой газ считается основным виновником парникового эффекта?\n 1) Азот\n 2) Углекислый газ\n 3) Кислород",
            "2. Какой из следующих видов энергии является возобновляемым?\n 1) Нефть\n 2) Ветер\n 3) Уголь",
            "3. Что такое переработка?\n 1) Процесс использования вторичных материалов\n 2) Процесс выбрасывания мусора\n 3) Процесс сжигания отходов",
            "4. Какое количество пластика оказывается в океане каждый год?\n 1) 5 миллионов тонн\n 2) 8 миллионов тонн\n 3) 12 миллионов тонн",
            "5. Какое дерево является символом охраны окружающей среды?\n 1) Ель\n 2) Дуб\n 3) Секвойя"
        };

        if (quizState.CurrentQuestionIndex < questions.Length)
        {
            await botClient.SendTextMessageAsync(chatId, questions[quizState.CurrentQuestionIndex], cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, $"Викторина завершена! Ваш счет: {quizState.Score}/{questions.Length}", cancellationToken: cancellationToken);
            UserQuizzes.Remove(chatId);
            await ShowMainMenu(chatId, cancellationToken);
        }
    }

    private static async Task HandleQuizResponse(long chatId, string userResponse, CancellationToken cancellationToken)
    {
        string[] correctAnswers = { "2", "2", "1", "2", "3" };
        var quizState = UserQuizzes[chatId];

        if (quizState.CurrentQuestionIndex < correctAnswers.Length)
        {
            string correctAnswer = correctAnswers[quizState.CurrentQuestionIndex];
            if (userResponse == correctAnswer)
            {
                quizState.Score++;
                await botClient.SendTextMessageAsync(chatId, "Правильно!", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, $"Неправильно. Правильный ответ: {correctAnswer}.", cancellationToken: cancellationToken);
            }

            quizState.CurrentQuestionIndex++;
            await SendQuizQuestion(chatId, quizState, cancellationToken);
        }
    }

    private static async Task InformAboutEcologicalProblemsAsync(long chatId, CancellationToken cancellationToken)
    {
        string message = "Вот некоторые текущие экологические проблемы:\n" +
                         "1. Изменение климата: Повышение температуры на планете приводит к изменениям в климатических условиях, что вызывает более частые и интенсивные стихийные бедствия.\n" +
                         "2. Загрязнение: Загрязнение воздуха и воды представляет серьезную угрозу для здоровья людей и экосистем.\n" +
                         "3. Уничтожение лесов: Вырубка лесов для сельского хозяйства приводит к потере биоразнообразия.\n" +
                         "4. Угрозы биоразнообразию: Множество видов растений и животных находятся под угрозой исчезновения.";
        await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
        await ShowMainMenu(chatId, cancellationToken);
    }

    private static async Task GiveEcoTipsAsync(long chatId, CancellationToken cancellationToken)
    {
        string message = "Вот несколько советов по улучшению экологии:\n" +
                         "1. Используйте многоразовые сумки и бутылки.\n" +
                         "2. Выключайте свет и электрические приборы, когда они не нужны.\n" +
                         "3. Сортируйте отходы и участвуйте в переработке.";
        await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
        await ShowMainMenu(chatId, cancellationToken);
    }

    private static async Task FindEcoPointsAsync(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Сургут", "Нижневартовск", "Ханты-Мансийск" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendTextMessageAsync(chatId, "Выберите ваш город для поиска ближайших эко-пунктов:", replyMarkup: keyboard, cancellationToken: cancellationToken);
    }

    private static async Task HandleCitySelection(long chatId, string city, CancellationToken cancellationToken)
    {
        string message;

        switch (city)
        {
            case "сургут":
                message = "Эко-пункты в Сургуте:\n" +
                          "1. Пункт приёма пластика - ул. Ленина, 35\n" +
                          "2. Пункт приёма макулатуры - ул. Энергетиков, 22\n" +
                          "3. Пункт приёма стекла - ул. Мира, 10";
                break;
            case "нижневартовск":
                message = "Эко-пункты в Нижневартовске:\n" +
                          "1. Пункт приёма пластика - ул. Пушкина, 14\n" +
                          "2. Пункт приёма батареек - ул. Гагарина, 5\n" +
                          "3. Пункт приёма металла - ул. Строителей, 17";
                break;
            case "ханты-мансийск":
                message = "Эко-пункты в Ханты-Мансийске:\n" +
                          "1. Пункт приёма макулатуры - ул. Советская, 25\n" +
                          "2. Пункт приёма стекла - ул. Победы, 8\n" +
                          "3. Пункт приёма пластика - ул. Ленина, 40";
                break;
            default:
                message = "Извините, я не знаю такого города. Попробуйте выбрать из предложенных вариантов.";
                break;
        }

        await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
        await ShowMainMenu(chatId, cancellationToken);
    }
}