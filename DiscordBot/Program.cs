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

        public static SocketGuild guild;

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
            } catch (Exception e)
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

        private async Task HandleUserJoinedAsync(SocketGuildUser user)
        {
            if (user.IsBot) return;
            var ghostRole = user.Guild.Roles.FirstOrDefault(x => x.Name == "Ghosts");
            if (ghostRole != null) await user.AddRoleAsync(ghostRole);

            var DMs = await user.GetOrCreateDMChannelAsync();
            await DMs.SendMessageAsync(
                 $"Welcome to the **Buddies** server!\n" +
                "Please read the rules, change your nickname to your real name and ask me for your roles!\n" +
                "All commands are prefixed by \"!\"" +
                "Role commands are programming, graphics, design, pm, newcomer, 1st, 2nd, 3rd, 4th and alumni\n" +
                "So for example if you're in your first year in game design and programming, you could write:\n" +
                "!programming\n" +
                "!1st\n" +
                "You also type !help if you don't remember a command!"
            );
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            //ugly but needed
            if (guild == null)
            {
                guild = client.GetGuild(441370070711533588); //buddies discord server id
            }

            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(client, message);
            if (message.Author.IsBot) return;
            
            int argumentPosition = 0;
            if (message.HasStringPrefix("!", ref argumentPosition) 
                || message.HasStringPrefix("frog ", ref argumentPosition) 
                || message.HasStringPrefix("Oh great FromgBot, ", ref argumentPosition)
                )
            {
                var result = await commands.ExecuteAsync(context, argumentPosition, services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
