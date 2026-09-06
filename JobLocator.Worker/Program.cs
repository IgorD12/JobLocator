using HtmlAgilityPack;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot; // подключение библиотеки Telegram.Bot для работы с Telegram API
using Telegram.Bot.Polling; 

string botToken = "********"; //токен бота|Hidden for safety purpose 

//long chatId = *******; Hidden for safety purpose 

var bot = new TelegramBotClient(botToken); // создаем экземпляр TelegramBotClient с токеном бота

var me = await bot.GetMe(); // получаем информацию о боте

Console.WriteLine($"Бот запущен: @{me.Username}"); // уведомление о запуске бота в консоль

List<JobPosting> oldJobs = new List<JobPosting>();
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = []
};

using var cts = new CancellationTokenSource();

locatingstart:

bot.StartReceiving(
    async (botClient, update, cancellationToken) =>
    {
        if (update.Message?.Text == null)
            return;

        string text = update.Message.Text.Trim().ToLower();

        if (text == "да" ||
            text == "давай" ||
            text == "го" ||
            text == "газ" ||
            text == "кнчн" ||
            text == "конечно" ||
            text == "валяй" ||
            text == "делай")
        {
            foreach (JobPosting job in oldJobs)
            {
                string message =
                    $"💼 {job.Title}\n\n" +
                    $"🏢 {job.HiringOrganization?.Name}\n" +
                    $"📍 {job.JobLocation?.Address?.AddressLocality}\n" +
                    $"💰 {job.BaseSalary?.Value?.MinValue} - {job.BaseSalary?.Value?.MaxValue} {job.BaseSalary?.Currency}\n\n" +
                    $"🔗 {job.Url}";

                await botClient.SendMessage(
                    chatId,
                    message,
                    cancellationToken: cancellationToken
                );

                await Task.Delay(750);

                
            }
        }
    },
    async (botClient, exception, cancellationToken) =>
    {
        Console.WriteLine(exception);
        await Task.CompletedTask;
    },
    receiverOptions,
    cts.Token
);

//Console.WriteLine("Ожидаю сообщения...");
//Console.ReadLine();

HttpClient client = new HttpClient();

string url = "https://www.prace.cz/nabidky/plny-uvazek/?keywords%5B%5D=IT&workLocationIds%5B%5D=M249832%3B10&education%5B%5D=HIGH_SCHOOL&salaryMin=20000&salaryCurrency=CZK"; // переменная в которую записывается url адрес

try
{
    List<JobPosting> jobs = new List<JobPosting>();
    
    List<SentJob> sentJobs = new List<SentJob>();

    if (File.Exists("sentJobs.json"))
    {
        string json = File.ReadAllText("sentJobs.json");

        sentJobs = JsonSerializer.Deserialize<List<SentJob>>(json)
                   ?? new List<SentJob>();


    }




    string[] excludedWords = File.ReadAllLines("exclude.txt");
    string[] includedWords = File.ReadAllLines("include.txt");

    for (int page = 1; page <= 3; page++)
    {
        string pageUrl = url + $"&page={page}";

        Console.WriteLine($"========== СТРАНИЦА {page} ==========");

        HttpResponseMessage response = await client.GetAsync(pageUrl);

        // дальше твой существующий код

        Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}"); // вывод статуса ответа
        Console.WriteLine($"Content-Length: {response.Content.Headers.ContentLength}"); // вывод длины содержимого ответа

        Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType}"); // вывод типа содержимого ответа

        string html = await response.Content.ReadAsStringAsync(); // чтение содержимого ответа в виде строки

        Console.WriteLine($"Получено символов: {html.Length}"); // вывод количества символов в полученном html коде

        File.WriteAllText("prace.html", html);

        Console.WriteLine("HTML сохранён в prace.html");


        HtmlDocument document = new HtmlDocument(); // переменная в которую записывается HTML-документ

        document.LoadHtml(html); // Загружаем полученный HTML в парсер

        Console.WriteLine("HTML загружен в HtmlAgilityPack");


        var links = document.DocumentNode.SelectNodes("//a"); // Ищем все элементы <a> на странице (просто проверка парсера)

        Console.WriteLine($"Найдено ссылок: {links?.Count ?? 0}");

        //bool jobFound = false; // временная стоп кран для поиска вакансий


        foreach (var link in links) // Для каждый ссылки в Links(перменная для хранения ссылок)
        {

            string href = link.GetAttributeValue("href", ""); //переменнная которая сохраняет в себе href и url ссылку
            string text = link.InnerText.Trim(); // перемнная которая сохраняет в себе текст гиперссылки 
            //------------------------------------------------------------------------//
            //                                                                        //
            //                                                                        //
            //------------------------------------------------------------------------//
            // на этапе ниже происходит проверка на то что ссылка является ссылкой на вакнасию.
            // В большинстве ссылок на вакансию есть "/nabidka/" в начале ссылки.
            if (href.StartsWith("/nabidka/"))
            {
                Console.WriteLine($"Текст: {text}"); // вывод текста гиперссылки (переменная text)
                Console.WriteLine($"Ссылка: {href}"); // вывод ссылки (переменная href)
                Console.WriteLine("--------------------"); // разделитель текста и ссылки

                // Формируем полный адрес вакансии
                string jobUrl = "https://www.prace.cz" + href;

                


                HttpResponseMessage jobResponse = await client.GetAsync(jobUrl); // Загружаем страницу вакансии

                Console.WriteLine($"Статус страницы вакансии: {jobResponse.StatusCode}"); // тут и так понятно

                string jobHtml = await jobResponse.Content.ReadAsStringAsync(); // Читаем содержимое страницы вакансии в виде строки

                Console.WriteLine($"Получено символов страницы вакансии: {jobHtml.Length}"); //тут понятно

                File.WriteAllText("job.html", jobHtml); // Сохраняем страницу вакансии в файл job.html

                Console.WriteLine("Страница вакансии сохранена в job.html");


                HtmlDocument jobDocument = new HtmlDocument(); // Создаем новый HTML-документ для страницы вакансии
                jobDocument.LoadHtml(jobHtml); // Загружаем HTML страницы вакансии в парсер

                var jobData = jobDocument.DocumentNode.SelectSingleNode( // Ищем структурированные данные вакансии
                    "//script[@type='application/ld+json']"
                );


                if (jobData != null) // Если структурированные данные вакансии найдены
                {
                    Console.WriteLine("JSON-LD вакансии найден!");
                    //Console.WriteLine(jobData.InnerText); отображает весь JSON-LD код вакансии в консоль (раскомментировать для отладки)

                    // Десериализуем (превращаем данные в объект) JSON-LD в объект JobPosting
                    JobPosting? job = JsonSerializer.Deserialize<JobPosting>(
                        jobData.InnerText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );


                    if (job != null)
                    {
                        job.Url = new Uri(new Uri("https://www.prace.cz"), href).ToString();



                        bool excluded = false;
                        bool included = false;

                        foreach (string word in includedWords)
                        {
                            if (ContainsWord(job.Title, word) || ContainsWord(job.Description, word))
                            {
                                included = true;
                                Console.WriteLine($"ВКЛЮЧЕНО: {job.Title} | Причина: {word}");
                                break;
                            }
                        }

                        foreach (string word in excludedWords)
                        {
                            if (ContainsWord(job.Title, word) || ContainsWord(job.Description, word))
                            {
                                excluded = true;
                                Console.WriteLine($"ИСКЛЮЧЕНА: {job.Title} | Причина: {word}");
                                break;
                            }
                        }
                        if (included || !excluded)
                        {
                            jobs.Add(job);
                        }

                        

                    }







                    HtmlDocument descriptionDocument = new HtmlDocument();

                    descriptionDocument.LoadHtml(job?.Description);

                    // Знак $ означает что в строке можно использовать переменные, а знак ? означает что если job равен null то не будет ошибки, а просто выведет null

                    string cleanDescription = descriptionDocument.DocumentNode.InnerText.Trim(); // очистка описания вакансии от HTML-тегов и лишних пробелов
                    Console.WriteLine($"Название вакансии: {job?.Title}"); //знаки вопроса означают что если job равен null то не будет ошибки, а просто выведет null
                    Console.WriteLine("|----------------------------------------------------------------------------------------------------------------------|");
                    Console.WriteLine($"Описание: {cleanDescription.Substring(0, 100)}..."); // вывод первых 100 символов описания вакансии
                    Console.WriteLine("|----------------------------------------------------------------------------------------------------------------------|");
                    Console.WriteLine($"Компания: {job?.HiringOrganization?.Name}");
                    Console.WriteLine("|----------------------------------------------------------------------------------------------------------------------|");
                    Console.WriteLine($"Город: {job?.JobLocation?.Address?.AddressLocality}");
                    Console.WriteLine("|----------------------------------------------------------------------------------------------------------------------|");
                    Console.WriteLine($"Зарплата: {job?.BaseSalary?.Value?.MinValue} - {job?.BaseSalary?.Value?.MaxValue} {job?.BaseSalary?.Currency}");
                    Console.WriteLine("|----------------------------------------------------------------------------------------------------------------------|");
                    Console.WriteLine($"Ссылка: {job?.Url}");



                }
                else // Если структурированные данные вакансии не найдены
                {
                    Console.WriteLine("JSON-LD вакансии не найден.");
                }

                //jobFound = true; // срыв стоп крана

                //if (jobFound)
                //{
                //    break; //ну всё, пизда. Отмена парсинга 
                //}
                Console.WriteLine($"Всего собрано вакансий: {jobs.Count}");

                
            }
            


        }

        Console.WriteLine($"=================================");
        Console.WriteLine($"ВСЕГО ПОДОБРАНО: {jobs.Count}");
        Console.WriteLine($"=================================");

        //foreach (var job in jobs)
        //{
        //    Console.WriteLine($"{job.Title} | {job.JobLocation?.Address?.AddressLocality} | {job.Url}");
        //}
    }

    Console.WriteLine($"Всего собрано вакансий: {jobs.Count}");



    //List<JobPosting> oldJobs = new List<JobPosting>();

    foreach (JobPosting job in jobs)
    {
        SentJob? sentJob = sentJobs.Find(x => x.Url == job.Url);

        if (sentJob != null && sentJob.SentAt >= DateTime.Now.AddDays(-3))
        {
            oldJobs.Add(job);
            continue;
        }

        string message =
            $"💼 {job.Title}\n\n" +
            $"🏢 {job.HiringOrganization?.Name}\n" +
            $"📍 {job.JobLocation?.Address?.AddressLocality}\n" +
            $"💰 {job.BaseSalary?.Value?.MinValue} - {job.BaseSalary?.Value?.MaxValue} {job.BaseSalary?.Currency}\n\n" +
            $"🔗 {job.Url}";

        await bot.SendMessage(
            chatId,
            message
        );
        sentJobs.Add(new SentJob
        {
            Url = job.Url,
            SentAt = DateTime.Now
        });

        Console.WriteLine($"Отправлено в Telegram: {job.Title}");

        
    }

    if (oldJobs.Count > 0)
    {
        await bot.SendMessage(
            chatId,
            $"Несколько вакансий уже были предложены ранее ({oldJobs.Count} шт.). Хочешь посмотреть их ещё раз?"
        );
        
    }

    string sentJobsJson = JsonSerializer.Serialize(sentJobs,new JsonSerializerOptions // сериализация списка отправленных вакансий в JSON
    {
        WriteIndented = true
    });

    File.WriteAllText("sentJobs.json", sentJobsJson);

}
catch (Exception ex) // Обработка исключений 
{
    Console.WriteLine("ОШИБКА:"); // извинения перед чечней
    Console.WriteLine(ex.ToString());   
}

System.Threading.Thread.Sleep(10800000);
Console.WriteLine("Начинаю заново");
goto locatingstart;




Console.ReadLine();


bool ContainsWord(string text, string word) // проверка если текст содержит слово из списка исключений или включений, с учетом регистра и границ слова
{
    return Regex.IsMatch(
        text,
        $@"\b{Regex.Escape(word)}\b",
        RegexOptions.IgnoreCase
    );
}

class SentJob // класс для хранения информации о ранее отправленных вакансиях
{
    public string Url { get; set; }
    public DateTime SentAt { get; set; }
}

