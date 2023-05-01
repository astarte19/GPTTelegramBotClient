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

        private static string apiKey = "your_api_key";
        private static string endpoint = "https://api.openai.com/v1/chat/completions";
        

        [Action("/start", "Меню")]
        public async Task Start()
        {           
            PushL($"✋ Привет, {Context.GetUserFullName()}!\n\n⚪ <b>Это Telegram-клиент для OpenAI ChatGPT!</b>");           
            RowButton("📱 Начать диалог",Q(SendRequestAndGetResponse));                               
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
