using System;
using Deployf.Botf;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot;
using System.Net;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using System.Data.SQLite;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;


namespace GPTTelegramBotClient
{
    
    public class MainController : BotController
    {

        private static string apiKey = "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";
        private static string endpoint = "https://api.openai.com/v1/chat/completions";
        private static string DBConnection = "Data Source=UsersDB.db;";

        [Action("/start", "Меню")]
        public async Task Start()
        {
            
            PushL($"✋ Привет, {Context.GetUserFullName()}!\n\n⚪ <b>Это клиент для Chat GPT!</b>");
            bool HasAccess = false;
            bool IsAdmin = false;

            SQLiteConnection DB = new SQLiteConnection(DBConnection);
            DB.Open();
            SQLiteCommand cmd = DB.CreateCommand();
            cmd.CommandText = $"SELECT * FROM Users WHERE UserId = '{ChatId.ToString()}'";
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                HasAccess = Convert.ToInt32(reader["HasAccess"]) == 1 ? true : false;
                IsAdmin = Convert.ToInt32(reader["IsAdmin"]) == 1 ? true : false;
            }
            DB.Close();
            if (HasAccess)
            {
                if(IsAdmin)
                {
                    PushL("Панель управления пользователями");                    
                    RowButton("💁 Показать пользователей",Q(ShowUsers));
                    RowButton("✅ Добавить пользователя");
                    RowButton("❌ Удалить пользователя");
                    RowButton("📱 Начать диалог", Q(SendRequestAndGetResponse));

                }
                else
                {
                    RowButton("📱 Начать диалог",Q(SendRequestAndGetResponse));

                }               
            }
            else
            {
                PushL("Для получения доступа обратитесь к администратору!");
                RowButton("⏪ Меню", Q(Start));
            }
                      
        }
        [Action]
        public async Task SendRequestAndGetResponse()
        {
                    
            while (true)
            {
                PushL("📖 Введите запрос:");
                await Send();
                string requestText = await AwaitText();
                if (requestText.Equals("/start"))
                {
                    PushL("⛔ Диалог остановлен.");
                    await Send();
                    break;                    
                }
                PushL("⌛ Ожидание ответа...");
                await Send();
                string responseText = await SendRequest(requestText);
                PushL(responseText);
                await Send();
            }
           
        }
        [Action]
        public async Task ShowUsers()
        {
           
            SQLiteConnection DB = new SQLiteConnection(DBConnection);
            DB.Open();
            SQLiteCommand cmd = DB.CreateCommand();
            cmd.CommandText = $"SELECT rowid,HasAccess,IsAdmin,Role,UserId FROM Users";
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string HasAccess = Convert.ToInt32(reader["HasAccess"]) == 1 ? "Yes" : "No";
                string IsAdmin = Convert.ToInt32(reader["IsAdmin"]) == 1 ? "Yes" : "No";
                PushL($"#{Convert.ToInt32(reader["rowid"])}\nRole: {reader["Role"].ToString()}\nHasAccess: {HasAccess}\nIsAdmin: {IsAdmin}\nChatID: {reader["UserId"].ToString()}");
            }
            DB.Close();
        }
        public async Task<string> SendRequest(string requestText)
        {
            List<Message> messages = new List<Message>();
            var httpClient = new HttpClient();            
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");                       
            var message = new Message() { Role = "user", Content = requestText };
            messages.Add(message);
            var requestData = new Request()
            {
                ModelId = "gpt-3.5-turbo",
                Messages = messages
            };
            using var response = await httpClient.PostAsJsonAsync(endpoint, requestData);
            if (!response.IsSuccessStatusCode)
            {               
                return $"{(int)response.StatusCode} {response.StatusCode}";
            }
            ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();

            var choices = responseData?.Choices ?? new List<Choice>();
            if (choices.Count == 0)
            {              
                return "API OpenAI ChatGPT ничего не ответило(";
            }
            var choice = choices[0];
            var responseMessage = choice.Message;            
            var responseText = responseMessage.Content.Trim();
            return responseText;

        }

    }
}
