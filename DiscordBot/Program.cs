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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

//delet
using System.Xml.Serialization;

namespace DiscordBot
{
    [Serializable]
    public enum Minors
    {
        Programming,
        Graphics,
        Design,
        PM
    }

    [Serializable]
    public enum Years
    {
        First,
        Second,
        Third,
        Masters,
        Alumni
    }
    public class Program
    {
        public static readonly string logFileName = "./DiscordBotLog.txt";
        public static readonly string minorRolesFileName = "./MinorRoles.xml";
        public static readonly string yearRolesFileName = "./YearRoles.xml";
        public static readonly string welcomeMessageFileName = "./WelcomeMessage.txt";
        public static readonly string helpMessageFileName = "./HelpMessage.txt";


        public static FileDictionary<Minors, ulong> minorRoles;
        public static FileDictionary<Years, ulong> yearRoles;
        public static FileString welcomeMessage;
        public static FileString helpMessage;

        private static FileStream ostrm;
        private static StreamWriter writer;
        private static TextWriter stdOut;

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
                ostrm = new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
                writer.AutoFlush = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot open \"{logFileName}\" for writing");
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

            //todo: check if they've all loaded successfully?
            minorRoles = new FileDictionary<Minors, ulong>("./MinorRoles.xml");
            yearRoles = new FileDictionary<Years, ulong>("./YearRoles.xml");
            welcomeMessage = new FileString(welcomeMessageFileName);
            helpMessage = new FileString(helpMessageFileName);

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

            IGuild[] guilds = { guild };
            await client.DownloadUsersAsync(guilds);

            var ghostRole = user.Guild.Roles.FirstOrDefault(x => x.Name == "Ghosts");
            if (ghostRole != null) await user.AddRoleAsync(ghostRole);

            var DMs = await user.GetOrCreateDMChannelAsync();
            await DMs.SendMessageAsync(welcomeMessage.Contents);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            //ugly but needed
            if (guild == null)
            {
                guild = client.GetGuild(441370070711533588); //buddies discord server id
            }

            var message = arg as SocketUserMessage;
            if (message == null) return; //this should never happen... but it has! So don't remove this :'D


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
