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
using System.Security.Policy;

namespace GPTTelegramBotClient
{
    
    public class MainController : BotController
    {

        private static string apiKey = "your_api_token";
        private static string endpoint = "https://api.openai.com/v1/chat/completions";
        private static string ConnectionString = "Data Source=UsersDB.db;";
        

        [Action("/start", "Меню")]
        public async Task Start()
        {
            bool hasAccess = HasAccess(Convert.ToInt64(Context.GetSafeUserId()));
            bool isAdmin = IsAdmin(Convert.ToInt64(Context.GetSafeUserId()));
            if (hasAccess)
            {                
                if(isAdmin)
                {
                    PushL($"👼 <strong>Приветствую, {Context.GetUsername()}</strong>");
                    RowButton("✅ Запустить сессию", Q(SendRequestAndGetResponse));
                    RowButton("📝 Список пользователей", Q(ShowUsers));
                    RowButton("➕ Добавить пользователя", Q(CreateUser));
                    RowButton("💀 Деактивировать пользователя", Q(DeactivateUser));
                    RowButton("☑️ Активировать пользователя", Q(ActivateUser));
                }
                else
                {
                    PushL("ℹ️ <strong>Для начала диалога с ChatGPT нажмите кнопку ниже.</strong>");
                    RowButton("✅ Запустить сессию", Q(SendRequestAndGetResponse));
                }
            }
            else
            {
                PushL("⛔ <strong>Отказано в доступе. Обратитесь к администратору для получения доступа</strong>");
                await Send();
            }
                      
        }
        
        [Action]
        public async Task SendRequestAndGetResponse()
        {
            PushL("✅ Сессия запущена");
            await Send();
            while (true)
            {
               
                string? requestText = await AwaitText();
               
                    if (requestText.Equals("/start"))
                    {
                        PushL("⛔ Сессия завершена");
                        await Send();
                        break;
                    }
                                   
                PushL("⌛");
                var message = await Send();
                string responseText = await SendRequest(requestText);
                Push(responseText); 
                MessageId = message.MessageId;
                await Update();               
            }
           
        }
        [Action]
        public async Task ShowUsers()
        {
            List<User> users = GetUsers();
            foreach (User item in users)
            {
                string Access = item.Active == true ? "✅" : "⛔";
                Push($"\n\nID: {item.Id}\nИмя пользователя: {item.UserName}\nДоступ: {Access}");
               
            }
            await Send();
        }
        [Action]
        public async Task DeactivateUser()
        {
            PushL("🆔 Введите ID пользователя:");
            await Send();
            string UserIDstr = await AwaitText();
            int UserID;
            bool isNumber = int.TryParse(UserIDstr, out UserID);
            if (!isNumber)
            {
                PushL("⛔ Некорректные данные");
                await Send();
                Start();
            }

            ActivateDeactivateUser(UserID, false);
            PushL("✅ Пользователю запрещен доступ");
            await Send();
        }
        [Action]
        public async Task ActivateUser()
        {
            PushL("🆔 Введите ID пользователя:");
            await Send();
            string UserIDstr = await AwaitText();
            int UserID;
            bool isNumber = int.TryParse(UserIDstr, out UserID);
            if (!isNumber)
            {
                PushL("⛔ Некорректные данные");
                await Send();
                Start();
            }

            ActivateDeactivateUser(UserID, true);
            PushL("✅ Пользователю разрешен доступ");
            await Send();
        }
        [Action]
        public async Task CreateUser()
        {
            PushL("🖊 Введите имя пользователя:");
            await Send();
            string UserName = await AwaitText();
            if (UserName.Equals("/start"))
            {
                PushL("⛔ Некорректные данные");
                await Send();
                Start();
            }
            PushL("🆔 Введите ID пользователя:");
            await Send();
            string ChatIDstr = await AwaitText();
            long ChatID;
            bool isNumber = long.TryParse(ChatIDstr, out ChatID);
            if (!isNumber)
            {
                PushL("⛔ Некорректные данные");
                await Send();
                Start();
            }

            AddUser(UserName, ChatID);
            PushL("✅ Пользователь добавлен");
            await Send();
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
        #region Administration 
        public bool HasAccess(long ChatID)
        {
            bool result = false;           
            string sql = $"SELECT Active FROM t_Users WHERE ChatID = {ChatID}";  
            
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!DBNull.Value.Equals(rdr["Active"]))
                            {
                                return Convert.ToBoolean(rdr["Active"]);
                            }

                        }
                    }
                }
            }
            return result;
        }
        public bool IsAdmin(long ChatID)
        {
            bool result = false;         
            string sql = $"SELECT IsAdmin FROM t_Users WHERE ChatID = {ChatID}";

            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (!DBNull.Value.Equals(rdr["IsAdmin"]))
                            {
                                return Convert.ToBoolean(rdr["IsAdmin"]);
                            }

                        }
                    }
                }
            }
            return result;
        }
        public void AddUser(string UserName,long ChatID)
        {            
            string sql = $"INSERT INTO t_Users(Username,Active,IsAdmin,ChatID) VALUES('{UserName}',true,false,{ChatID})";         
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void ActivateDeactivateUser(int UserID, bool Active)
        {
            string sql = $"UPDATE t_Users SET Active = {Active} WHERE rowid = {UserID}";
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public List<User> GetUsers()
        {
            List<User> result = new List<User>();           
            string sql = $"SELECT rowid,Username,Active FROM t_Users";
            
            using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            User user = new User();

                            if (!DBNull.Value.Equals(rdr["rowid"]))
                            {
                                user.Id = Convert.ToInt32(rdr["rowid"]);
                            }
                            if (!DBNull.Value.Equals(rdr["Username"]))
                            {
                                user.UserName = rdr["Username"].ToString();
                            }
                            if (!DBNull.Value.Equals(rdr["Active"]))
                            {
                                user.Active = Convert.ToBoolean(rdr["Active"]);
                            }
                            result.Add(user);
                        }
                    }
                }
            }
            return result;
        }
        #endregion

    }
}
