using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

class Program
{
    static ITelegramBotClient bot = new TelegramBotClient(constants.botId);
    static HttpClient httpClient = new HttpClient();
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
        {
            var message = update.Message;
            User user = message.From;
            string user_firstname = user.FirstName;
            long user_id = user.Id;

            var asteroids_list = new List<Asteroid>();

            var document = new BsonDocument
                    {
                        { "user_id", user_id},
                        { "user_firstname", user_firstname },
                {"user_is_subscribed_to_updates", false },
                {"bot_is_waiting_for_photo_data", false },
                {"bot_is_waiting_for_number_of_asteroid_to_add", false },
                {"bot_is_waiting_for_number_of_asteroid_to_delete", false },
                  {"asteroids", new BsonArray(asteroids_list.Select(t => t.ToBsonDocument())) }
                };

            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var exists = constants.collection.Find(filter).Any();

            if (!exists)
            {
                constants.collection.InsertOne(document);
            }

            var resp = await httpClient.GetAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_photo_data/{user_id}");
            var res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_photo_data = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/nasa_/user_is_subscribed_to_updates/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool user_is_subscribed_to_updates = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_add/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_number_of_asteroid_to_add = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_delete/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_number_of_asteroid_to_delete = Convert.ToBoolean(res);

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(user_id, "Привіт!\nЯ допоможу тобі дізнатись більше про космос, обирай, що цікаво, і давай досліджувати всесвіт разом");
                return;
            }
            if (message.Text.ToLower() == "/daily_photo")
            {
                await httpClient.PostAsync($"https://{constants.host}/nasa_/post_daily_photo?chatid={user_id}&datetime={DateTime.UtcNow.ToString("yyyy-MM-dd")}", null);
                if (!user_is_subscribed_to_updates)
                {
                    await botClient.SendTextMessageAsync(user_id, "Ви можете підписатися, щоб кожен день отримувати фото дня. Для цього використайте /subscribe");
                }
                return;
            }
            if (message.Text.ToLower() == "/daily_photo_by_date")
            {
                await botClient.SendTextMessageAsync(user_id, "Добре, введіть дату у вигляді рррр-мм-дд");
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_photo_data/{user_id}?b=true", null);
                return;
            }

            if (message.Text.ToLower() == "/subscribe")
            {
                if (user_is_subscribed_to_updates)
                {
                    await botClient.SendTextMessageAsync(user_id, "Ви вже підписалися на фото дня");
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Ви підписалися на фото дня");
                    await httpClient.PutAsync($"https://{constants.host}/nasa_/user_is_subscribed_to_updates/{user_id}?b=true", null);
                }
                return;
            }
            if (message.Text.ToLower() == "/unsubscribe")
            {
                if (!user_is_subscribed_to_updates)
                {
                    await botClient.SendTextMessageAsync(user_id, "Ви не підписані на фото дня");
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Ви відписалися від фото дня");
                    await httpClient.PutAsync($"https://{constants.host}/nasa_/user_is_subscribed_to_updates/{user_id}?b=false", null);
                }
                return;
            }
            if (message.Text.ToLower() == "/asteroids")
            {
                await httpClient.PostAsync($"https://{constants.host}/nasa_/post_asteroids_list?id={user_id}", null);
                return;
            }
            if (message.Text.ToLower() == "/add_asteroid_to_my_list")
            {
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_add/{user_id}?b=true", null);
                await bot.SendTextMessageAsync(user_id, "Добре, введіть номер астероїду, який ви хочете додати у свій список");
                return;
            }
            if (message.Text.ToLower() == "/delete_asteroid_from_my_list")
            {
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_delete/{user_id}?b=true", null);
                await bot.SendTextMessageAsync(user_id, "Добре, введіть номер астероїду, який ви хочете видалити зі свого списку");
                return;
            }
            if (message.Text.ToLower() == "/my_asteroids")
            {
                await httpClient.PostAsync($"https://{constants.host}/nasa_/post_my_asteroids_list?id={user_id}", null);
                return;
            }
            if (bot_is_waiting_for_number_of_asteroid_to_add)
            {
                string number = message.Text;
                try
                {
                    int i = Convert.ToInt32(number);
                    await httpClient.PutAsync($"https://{constants.host}/nasa_/put_asteroid_to_list?id={user_id}&number={number}", null);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                }
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_add/{user_id}?b=false", null);
                return;
            }
            if (bot_is_waiting_for_number_of_asteroid_to_delete)
            {
                string number = message.Text;
                try
                {
                    int i = Convert.ToInt32(number);
                    await httpClient.DeleteAsync($"https://{constants.host}/nasa_/delete_asteroid_from_list?id={user_id}&number={number}");
                }
                catch
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                }
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_number_of_asteroid_to_delete/{user_id}?b=false", null);
                return;
            }

            if (bot_is_waiting_for_photo_data)
            {
                string data = message.Text;
                string format = "yyyy-MM-dd";
                DateTime parsedDate;

                if (DateTime.TryParseExact(data, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                {
                    await httpClient.PostAsync($"https://{constants.host}/nasa_/post_daily_photo?chatid={user_id}&datetime={data}", null);
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                }
                await httpClient.PutAsync($"https://{constants.host}/nasa_/bot_is_waiting_for_photo_data/{user_id}?b=false", null);
                return;
            }
            await botClient.SendTextMessageAsync(user_id, "Я не розумію, що ти хочеш");
            return;
        }
        else
        {
            if (update.Message != null && update.Message.From != null)
            {
                long user_id = update.Message.From.Id;
                await botClient.SendTextMessageAsync(user_id, "Я розрахований лише на текстові повідомлення");
            }
        }
    }
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }




    public static async Task DailyUpdate()
    {
        ITelegramBotClient bott = new TelegramBotClient(constants.botId);
        while (true)
        {
            if (DateTime.UtcNow.Hour == 10 && DateTime.UtcNow.Minute == 0)
            {
                var filter = Builders<BsonDocument>.Filter.Empty;
                var documents = constants.collection.Find(filter).ToList();

                foreach (var document in documents)
                {
                    if (Convert.ToBoolean(document["user_is_subscribed_to_updates"]) == true)
                    {
                        long user_id = Convert.ToInt64(document["user_id"]);
                        await httpClient.PostAsync($"https://{constants.host}/nasa_/post_daily_photo?chatid={user_id}&datetime={DateTime.UtcNow.ToString("yyyy-MM-dd")}", null);
                        bott.SendTextMessageAsync(user_id, "Ви завжди можете відписатися від фото дня, використавши команду /unsubscribe");
                    }
                }
                Thread.Sleep(80000);
            }
        }
    }






    static void Main(string[] args)
    {

        Task.Run(async () => await DailyUpdate());

        Console.WriteLine("Запущен бот" + bot.GetMeAsync().Result.FirstName);

        constants.mongoClient = new MongoClient("mongodb+srv://ddd:polki@cluster0.za7o538.mongodb.net/");
        constants.database = constants.mongoClient.GetDatabase("nasa");
        constants.collection = constants.database.GetCollection<BsonDocument>("collection1");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
        //Console.ReadLine();
    }
}