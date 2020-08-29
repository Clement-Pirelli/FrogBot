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

namespace DiscordBot.Modules
{
    class Commands : ModuleBase<SocketCommandContext>
    {
        private bool IsValidRoleRequest(IGuildUser user, SocketRole role)
        {
            bool yearRole = Program.yearRoles.Data.ContainsValue(role.Id);
            bool minorRole = Program.minorRoles.Data.ContainsValue(role.Id);
            
            if (!yearRole && !minorRole) return true;

            foreach (var id in user.RoleIds)
            {
                if (id == role.Id) continue;

                if (yearRole)
                {
                    if (Program.yearRoles.Data.ContainsValue(id))
                        return false;
                }
                else //minor role
                {
                    //masters students dont have minors
                    if(id == Program.yearRoles[Years.Masters] || Program.minorRoles.Data.ContainsValue(id))
                        return false;
                }
            }

            return true;
        }

        private string GetFormattedUsername()
        {
            return Context.User.Username + "#" + Context.User.Discriminator;
        }

        private async Task SendConfirmation()
        {
            if (Emote.TryParse("<:frogthumbsup:733751715160915968>", out var emote))
            {
                await Context.Message.AddReactionAsync(emote);
            }
            else
            {
                await Context.Message.AddReactionAsync(new Emoji(char.ConvertFromUtf32(0x1F44D)));
            }
        }
        private async Task SendDenial() 
        {
            if (Emote.TryParse("<:frogthumbsdown:733751715098001509>", out var emote))
            {
                await Context.Message.AddReactionAsync(emote);
            }
            else
            {
                await Context.Message.AddReactionAsync(new Emoji(char.ConvertFromUtf32(0x1F44E)));
            }
        }

        private async Task SendToModeratorChat(string message, SocketGuild guild) 
        {
            Console.WriteLine(message);
            var moderatorChannel = guild.GetTextChannel(592090738901254145);
            await moderatorChannel.SendMessageAsync(message);
        }

        private async Task<IGuildUser> GetUserFromGuild(SocketGuild guild) 
        {
            SocketGuildUser buddiesUser = null;
            for (int i = 0; i < 10; i++) //try 10 times, this is needed because we might get a false negative... thanks discord.net, very cool
            {
                buddiesUser = guild.GetUser(Context.User.Id);
                if (buddiesUser != null) break;
                await Task.Delay(1000);
            }

            if (buddiesUser == null) //user isn't found in the guild
            {
                var message = $"User {GetFormattedUsername()} is trying to use the bot but they're not in the server.";
                await SendToModeratorChat(message, guild);
                await Context.Channel.SendMessageAsync("I'm really sorry but something went terribly wrong in toggling your role :( Don't worry though, a moderator will see that this operation has failed and hopefully add your role manually soon. \nApologies!");
                return null;
            }

            var guildUser = buddiesUser as IGuildUser; //this should always work. If it doesn't, bail out immediately because something went insanely wrong
            if (guildUser == null)
            {
                var message = $"User {GetFormattedUsername()} was found in the guild but isn't an IGuildUser???????";
                Console.WriteLine(message);
                return null;
            }

            return guildUser;
        }

        private async Task ToggleRole(ulong id)
        {
            try 
            {
                var buddiesGuild = Program.guild;

                if (buddiesGuild == null)
                {
                    throw new Exception("Buddies hasn't been set. Bailing out!");
                }

                //try to get the user
                var guildUser = await GetUserFromGuild(buddiesGuild);
                if (guildUser == null)
                {
                    var message = $"User {GetFormattedUsername()}'s GuildUser could not be retrieved for role : { buddiesGuild.GetRole(id).Name }";
                    await SendToModeratorChat(message, buddiesGuild);
                    throw new Exception(message);
                }

                SocketRole role, ghostRole;
                ghostRole = Program.guild.GetRole(496632805820727298); //ghost role id
                role = buddiesGuild.GetRole(id);

                //check if the request is valid
                if (!IsValidRoleRequest(guildUser, role))
                {
                    throw new Exception($"User {GetFormattedUsername()}'s request for role: {buddiesGuild.GetRole(id).Name} is not valid!");
                }

                //remove ghost role if the user has it
                if (guildUser.RoleIds.Contains(ghostRole.Id)) await guildUser.RemoveRoleAsync(ghostRole);

                if (guildUser.RoleIds.Contains(role.Id))
                {
                    Console.WriteLine($"Removed role {role.Name} from user {guildUser.Username + "#" + guildUser.Discriminator}, id {guildUser.Id}");
                    await guildUser.RemoveRoleAsync(role);
                }
                else
                {
                    Console.WriteLine($"Added role {role.Name} to user {guildUser.Username + "#" + guildUser.Discriminator}, id {guildUser.Id}");
                    await guildUser.AddRoleAsync(role);
                }

                await SendConfirmation();
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
                await SendDenial();
            }
        }

        //this is sad code but eh well.
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

        [Command("masters")]
        public async Task Masters() => await ToggleRole(Program.yearRoles[Years.Masters]);

        [Command("alumni")]
        public async Task Alumni() => await ToggleRole(Program.yearRoles[Years.Alumni]);


        [Command("help")]
        public async Task Help() => await ReplyAsync(Program.helpMessage.Contents);

        private async Task ExecuteAdminCommand(Func<Task> task)
        {
            var user = await GetUserFromGuild(Program.guild);
            if (user != null && user.GuildPermissions.Administrator)
            {
                await task();

                await SendConfirmation();
            }
            else
            {
                await SendDenial();
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task ExecuteAdminCommand(Action action) => await ExecuteAdminCommand(async () => action());
#pragma warning restore CS1998

        [Command("cleanup")]
        public async Task Cleanup(int amount)
        {
            if (amount <= 0) amount = 100;

            await ExecuteAdminCommand(async () => 
            {
                var requestOptions = new RequestOptions();
                var reason = $"Bot cleanup";
                requestOptions.AuditLogReason = reason;
                Console.WriteLine(reason);
                var messagesRequest = Context.Channel.GetMessagesAsync(amount, options: requestOptions);
                await foreach (var messages in messagesRequest)
                {
                    foreach (var message in messages)
                    {
                        if (message.Content.StartsWith('!') || message.Author.IsBot)
                        {
                            await Context.Channel.DeleteMessageAsync(message);
                            await Task.Delay(100);
                        }
                    }
                }
            });
        }

        [Command("flushlog")]
        public async Task FlushLog() => await ExecuteAdminCommand(Console.Clear);

        [Command("reloadfiles")]
        public async Task Reload() => 
            await ExecuteAdminCommand(() =>
            {
                //todo: it's fine for now to reload manually but at some point we might want to iterate over a collection of FileData
                Program.yearRoles.Reload();
                Program.minorRoles.Reload();
                Program.helpMessage.Reload();
                Program.welcomeMessage.Reload();
            });
    }
}
