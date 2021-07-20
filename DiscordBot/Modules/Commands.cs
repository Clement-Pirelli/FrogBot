using System;
using System.Collections.Generic;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord;

namespace DiscordBot.Modules
{
    class Commands : ModuleBase<SocketCommandContext>
    {
        [RequireContext(ContextType.Guild)]
        [Command("cleanup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Cleanup()
        {
            const int amount = 100;
            const String reason = "Bot cleanup";

            var requestOptions = new RequestOptions();
            requestOptions.AuditLogReason = reason;
            await Utilities.SendLog(reason, Context.Guild);
            var messagesRequest = Context.Channel.GetMessagesAsync(amount, options: requestOptions);
            await foreach (var messages in messagesRequest)
            {
                List<IMessage> messageList = new List<IMessage>();
                foreach (var message in messages)
                {
                    if ((message.Content.StartsWith('!') || message.Author.IsBot) &&
                        message.CreatedAt.CompareTo(DateTimeOffset.Now.AddDays(-14.0)) > 0) //make sure it's within two weeks
                    {
                        messageList.Add(message);
                    }
                }
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messageList);
            }
        }

        [Command("flushlog")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task FlushLog() 
        {
            Console.Clear();
            await Utilities.ConfirmMessage(Context.Message); 
        }

        [Command("reloadfiles")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task Reload() 
        {
            Program.emotesToRoleIds.Reload();
            await Utilities.ConfirmMessage(Context.Message);

            var log = "Loaded key-value pairs are:\n";
            foreach(var keyValuePair in Program.emotesToRoleIds.Data) 
            {
                log += $"{keyValuePair.Key} - {keyValuePair.Value}\n";
            }
            await Utilities.SendLog(log, Context.Guild);
        }

        [Command("getghosts")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageRoles)]
        public async Task GetGhosts() 
        {
            string message = "";
            var buddies = Utilities.BuddiesGuild(Context.Client);
            foreach (var user in Utilities.GetGhostsAndNoRoles(buddies)) 
            {
                message += user.ToString() + " ";
            }
            await Utilities.SendModerators(message, buddies);
        }

        [Command("kickghosts")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageRoles)]
        public async Task KickGhosts() 
        {
            await foreach(var user in Utilities.GetGhostsAndNoRoles(Context.Guild).ToAsyncEnumerable()) 
            {
                await user.KickAsync();
            }
        }

        [Command("say")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Say(string message)
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(message);
            await Utilities.SendLog($"User { Context.User } said : \" { message } \" in channel #{ Context.Channel.Name }", Context.Guild);
        }

        [Command("edit")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Edit(ulong messageId, string newMessage)
        {
            var message = await Context.Channel.GetMessagesAsync().Flatten().FirstAsync(x => x.Id == messageId);
            var socketMessage = message as Discord.WebSocket.SocketUserMessage;
            var restMessage = message as Discord.Rest.RestUserMessage;
            if (socketMessage == null && restMessage == null) 
            {
                await Utilities.SendLog($"Couldn't find or edit message with ID {messageId}", Context.Guild);
                await Context.Message.DeleteAsync();
                return;
            }
            if (restMessage != null) 
            {
                await restMessage.ModifyAsync(x => x.Content = newMessage);
            }
            else
            {
                await socketMessage.ModifyAsync(x => x.Content = newMessage);
            }
            await Utilities.SendLog($"User { Context.User } modified message with ID {messageId} to say:\n \" { newMessage } \"", Context.Guild);
            await Context.Message.DeleteAsync();
        }
    }
}
