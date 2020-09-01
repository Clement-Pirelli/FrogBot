using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Net.Sockets;

namespace DiscordBot.Modules
{
    class Commands : ModuleBase<SocketCommandContext>
    {
        private const ulong buddiesId = 441370070711533588;
        public async Task ToggleRole(ulong id)
        {
            try
            {
                var buddiesGuild = Context.Client.GetGuild(buddiesId);
                if (buddiesGuild == null)
                {
                    throw new Exception("Buddies can't be found. Bailing out!");
                }

                //try to get the user
                IGuildUser guildUser = null;
                guildUser = Context.User as IGuildUser;

                //if we're in DMs
                if (guildUser == null)
                {
                    guildUser = await Utilities.GetUserFromGuild(buddiesGuild, Context.User);
                }

                if (guildUser == null)
                {
                    var message = $"User @{Context.User}'s GuildUser could not be retrieved for role : { buddiesGuild.GetRole(id).Name }";
                    await Utilities.SendToModeratorChat(message, buddiesGuild); 
                    await Context.Channel.SendMessageAsync("I'm really sorry but something went terribly wrong in toggling your role :( Don't worry though, a moderator will see that this operation has failed and hopefully add your role manually soon. \nApologies!");
                    throw new Exception(message);
                }

                SocketRole role, ghostRole;
                ghostRole = buddiesGuild.GetRole(496632805820727298); //ghost role id
                role = buddiesGuild.GetRole(id);

                //check if the request is valid
                if (!Utilities.IsValidRoleRequest(guildUser, role))
                {
                    throw new Exception($"User @{Context.User}'s request for role: {buddiesGuild.GetRole(id).Name} is not valid!");
                }

                //remove ghost role if the user has it
                if (guildUser.RoleIds.Contains(ghostRole.Id)) await guildUser.RemoveRoleAsync(ghostRole);

                if (guildUser.RoleIds.Contains(role.Id))
                {
                    Console.WriteLine($"Removed role {role.Name} from user {Context.User}, id {Context.User.Id}");
                    await guildUser.RemoveRoleAsync(role);
                }
                else
                {
                    Console.WriteLine($"Added role {role.Name} to user {Context.User}, id {Context.User.Id}");
                    await guildUser.AddRoleAsync(role);
                }

                await Utilities.SendConfirmation(Context.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await Utilities.SendDenial(Context.Message);
            }
        }


        //this is sad code but eh well, TODO make it better :')
        [Command("programming")]
        public async Task Programmer() => await ToggleRole(Program.minorRoles[Minors.Programming]);

        [Command("graphics")]
        public async Task Graphics() => await ToggleRole(Program.minorRoles[Minors.Graphics]);

        [Command("design")]
        public async Task Design() => await ToggleRole(Program.minorRoles[Minors.Design]);

        [Command("pm")]
        public async Task PM() => await ToggleRole(Program.minorRoles[Minors.PM]);

        [Command("1st")]
        public async Task FirstYear() => await ToggleRole(Program.yearRoles[Years.First]);

        [Command("2nd")]
        public async Task SecondYear() => await ToggleRole(Program.yearRoles[Years.Second]);

        [Command("3rd")]
        public async Task ThirdYear() => await ToggleRole(Program.yearRoles[Years.Third]);

        [Command("masters1")]
        public async Task Masters() => await ToggleRole(Program.yearRoles[Years.Masters]);

        [Command("masters2")]
        public async Task MastersSecond() => await ToggleRole(Program.yearRoles[Years.MastersSecond]);

        [Command("alumni")]
        public async Task Alumni() => await ToggleRole(Program.yearRoles[Years.Alumni]);


        [Command("help")]
        public async Task Help() => await ReplyAsync(Program.helpMessage.Contents);

        [RequireContext(ContextType.Guild)]
        [Command("cleanup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Cleanup()
        {
            const int amount = 100;

            var requestOptions = new RequestOptions();
            var reason = $"Bot cleanup";
            requestOptions.AuditLogReason = reason;
            Console.WriteLine(reason);
            var messagesRequest = Context.Channel.GetMessagesAsync(amount, options: requestOptions);
            await foreach (var messages in messagesRequest)
            {
                List<IMessage> messageList = new List<IMessage>();
                foreach (var message in messages)
                {
                    if (message.Content.StartsWith('!') || message.Author.IsBot)
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
            await Utilities.SendConfirmation(Context.Message); 
        }

        [Command("reloadfiles")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task Reload() 
        {
            Program.yearRoles.Reload();
            Program.minorRoles.Reload();
            Program.helpMessage.Reload();
            Program.welcomeMessage.Reload();

            await Utilities.SendConfirmation(Context.Message);
        }
    }
}
