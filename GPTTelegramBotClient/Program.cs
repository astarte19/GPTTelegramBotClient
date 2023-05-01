using System;
using Deployf.Botf;
namespace GPTTelegramBotClient
{
    public class Program : BotfProgram
    {
        public static void Main(string[] args)
        {
            BotfProgram.StartBot(args, onConfigure: (svc, cfg) =>
            {
            }, onRun: (app, cfg) => {
                app.UseStaticFiles();
            });
        }
    }
}