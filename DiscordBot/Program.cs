using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.IO;

namespace DiscordBot
{
    class Program
    {
        public static readonly string logFileName = "DiscordBotLog.txt";
        public static FileStream ostrm;
        public static StreamWriter writer;
        public static TextWriter stdOut;

        public static readonly string welcomeMessage = "Welcome to the buddies server!\n" +
                " Please read the rules, change your nickname to your real name and ask me for your roles!\n" +
                "The current role commands are :\n" +
                "   !programming:         for the programming minor\n" +
                "   !graphics:            for the graphics minor\n" +
                "   !pm:                  for the project management minor\n" +
                "   !design:              for the design minor\n" +
                "   !newcomer:            for people starting the education after the summer\n" +
                "   !1st:                 for 1st year bachelors students\n" +
                "   !2nd:                 for 2nd year bachelors students\n" +
                "   !3rd:                 for 3rd year bachelors students\n" +
                "   !4th:                 for 1st year masters students\n" +
                "   !alumni:              for alumni\n" +
                "You can also ask for !help if you don't remember a command!";

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client = new DiscordSocketClient();
        private CommandService commands = new CommandService();
        private IServiceProvider services;

        public async Task RunBotAsync()
        {
            stdOut = Console.Out;
            try
            {
                ostrm = new FileStream($"./{logFileName}", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
                writer.AutoFlush = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot open {logFileName} for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.WriteLine("Successfully opened log file. Starting discordbot...");
            Console.SetOut(writer);
            Console.WriteLine(DateTime.Now);



            services = new ServiceCollection().AddSingleton(client).AddSingleton(commands).BuildServiceProvider();
            string token;
            try
            {
                token = File.ReadAllText("token");
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            client.Log += ClientLog;

            await RegisterCommandsAsync();

            await client.LoginAsync(TokenType.Bot, token);

            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task ClientLog(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            client.UserJoined += HandleUserJoinedAsync;
            await commands.AddModuleAsync(typeof(Modules.Commands), services);
        }

        private async Task HandleUserJoinedAsync(SocketGuildUser arg)
        {
            if (arg.IsBot) return;
            var ghostRole = arg.Guild.Roles.FirstOrDefault(x => x.Name == "Ghosts");
            if (ghostRole != null) await arg.AddRoleAsync(ghostRole);

            var generalChannel = client.GetChannel(441370070711533590) as SocketTextChannel;
            await generalChannel.SendMessageAsync(welcomeMessage);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(client, message);
            if (message.Author.IsBot) return;

            int argPos = 0;
            if(message.HasStringPrefix("!", ref argPos)) 
            {
                var result = await commands.ExecuteAsync(context, argPos, services);
                if (!result.IsSuccess) 
                {
                    Console.WriteLine(result.ErrorReason);
                }
            } else
            {
                if (message.HasStringPrefix("frog ", ref argPos))
                {
                    var result = await commands.ExecuteAsync(context, argPos, services);
                    if (!result.IsSuccess)
                    {
                        Console.WriteLine(result.ErrorReason);
                    }
                }
            }
        }
    }
}
