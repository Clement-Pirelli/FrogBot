using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IO;
using System.Collections.Generic;

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
        MastersSecond,
        Alumni
    }
    public class Program
    {
        public static readonly string logFileName = "./DiscordBotLog.txt";
        public static readonly string emotesToRoleIdsFileName = "./EmotesToRoleIds.xml";

        public static FileDictionary<string, ulong> emotesToRoleIds;

        private static FileStream ostrm;
        private static StreamWriter writer;

        private DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true });
        private CommandService commands = new CommandService();
        private IServiceProvider services;

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
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

            emotesToRoleIds = new FileDictionary<string, ulong>(emotesToRoleIdsFileName);

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
            client.ReactionAdded += (Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction) => HandleReactionAsync(cacheableMessage, channel, reaction, true);
            client.ReactionRemoved += (Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction) => HandleReactionAsync(cacheableMessage, channel, reaction, false); ;
            await commands.AddModuleAsync(typeof(Modules.Commands), services);
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel, SocketReaction reaction, bool added)
        {
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot) return;
            
            var user = reaction.User.Value;
            
            try {
               
                var roleId = emotesToRoleIds[reaction.Emote.Name];

                var socketUser = user as SocketUser;
                if (socketUser == null) return;

                await Utilities.ToggleRole(client, socketUser, roleId);
            } catch(KeyNotFoundException e) 
            {
                if (added && channel.Id == 751219349012086904) //Id of the role-setting channel
                {
                    var message = await cacheableMessage.GetOrDownloadAsync();
                    await message.RemoveReactionAsync(reaction.Emote, user);
                }
            }
        }

        private async Task HandleUserJoinedAsync(SocketGuildUser user)
        {
            if (user.IsBot) return;

            try
            {
                var ghostRole = user.Guild.Roles.FirstOrDefault(x => x.Name == "Ghosts");
                if (ghostRole != null) 
                {
                    await user.AddRoleAsync(ghostRole);
                } else
                {
                    throw new Exception("Ghost role couldn't be found!");
                }
            } catch(Exception e) 
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            //message == null should never happen... but it has! So don't remove this :'D
            if (message == null || message.Author.IsBot) return; 

            var context = new SocketCommandContext(client, message);            
            int argumentPosition = 0;
            if (message.HasStringPrefix("!", ref argumentPosition))
            {
                try 
                {
                    var result = await commands.ExecuteAsync(context, argumentPosition, services);
                    if (!result.IsSuccess)
                        throw new Exception(result.ErrorReason);
                } catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
