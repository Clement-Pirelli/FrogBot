using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using System.IO;

namespace DiscordBot.Modules
{
    class Commands : ModuleBase<SocketCommandContext>
    {

        private enum Minors
        {
            Programming, 
            Graphics, 
            Design, 
            PM
        }

        //todo: have these be read from a file we can upload to the server while it's running
        //then we could add a command like "hey, look at that file again I just changed it"
        private static readonly Dictionary<Minors, ulong> minorRoles = new Dictionary<Minors, ulong>()
        {
            { Minors.Programming,   471661457650483230 },
            { Minors.Graphics,      471661504542801921 },
            { Minors.Design,        471661568145358848 },
            { Minors.PM,            471661564613623808 }
        };

        private enum Years
        {
            Newcomer,
            First,
            Second,
            Third,
            Masters,
            Alumni
        }

        private static readonly Dictionary<Years, ulong> yearRoles = new Dictionary<Years, ulong>()
        {
           {Years.Newcomer, 709470722363359253 },
           {Years.First,    570962630701744138 },
           {Years.Second,   443902787172958219 },
           {Years.Third,    443901214908874755 },
           {Years.Masters,  596227227650097172 },
           {Years.Alumni,   443902508851527683 },
        };

        private bool IsValidRoleRequest(IGuildUser user, SocketRole role)
        {
            bool yearRole = yearRoles.ContainsValue(role.Id);
            bool minorRole = minorRoles.ContainsValue(role.Id);
            
            if (!yearRole && !minorRole) return true;

            foreach (var id in user.RoleIds)
            {
                if (id == role.Id) continue;

                if (yearRole)
                {
                    if (yearRoles.ContainsValue(id))
                        return false;
                }
                else //minor role
                {
                    //masters students dont have minors
                    if(id == yearRoles[Years.Masters] || minorRoles.ContainsValue(id))
                        return false;
                }
            }

            return true;
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

        private async Task<IGuildUser> GetUserFromGuild(SocketGuild guild) 
        {
            SocketGuildUser buddiesUser = null;
            for (int i = 0; i < 10; i++) //try 10 times, this is needed because we might get a false negative... thanks discord.net, very cool
            {
                buddiesUser = guild.GetUser(Context.User.Id);
                if (buddiesUser != null) break;
                await Task.Delay(100);
            }

            if (buddiesUser == null) //user isn't found in the guild
            {
                var message = $"User {Context.User.Username}#{Context.User.Discriminator} is trying to use the bot but they're not in the server.";
                Console.WriteLine(message);
                var moderatorChannel = guild.GetTextChannel(592090738901254145);
                await moderatorChannel.SendMessageAsync(message);
                return null;
            }

            var guildUser = buddiesUser as IGuildUser; //this should always work. If it doesn't, bail out immediately because something went insanely wrong
            if (guildUser == null)
            {
                var message = $"User {Context.User.Username}#{Context.User.Discriminator} was found in the guild but isn't an IGuildUser???????";
                Console.WriteLine(message);
                return null;
            }

            return guildUser;
        }

        private async Task ToggleRole(ulong id)
        {
            var buddiesGuild = Program.guild;

            if(buddiesGuild == null) 
            {
                Console.WriteLine("Buddies hasn't been set. Bailing out!");
                return;
            }

            //try to get the user
            var guildUser = await GetUserFromGuild(buddiesGuild);
            if (guildUser == null) return;

            SocketRole role, ghostRole;
            try 
            {
                ghostRole = Program.guild.GetRole(496632805820727298); //ghost role id
                role = buddiesGuild.GetRole(id);
            }
            catch (Exception exception) 
            {
                Console.WriteLine(exception.Message);
                await SendDenial();
                return;
            }

            //check if the request is valid
            if (!IsValidRoleRequest(guildUser, role)) 
            {
                await SendDenial();
                return;
            }
            
            //remove ghost role if the user has it
            if (guildUser.RoleIds.Contains(ghostRole.Id)) await guildUser.RemoveRoleAsync(ghostRole);

            if (guildUser.RoleIds.Contains(role.Id))
            {
                Console.WriteLine($"Removed role {role.Name} from user {guildUser.Username + "#" + guildUser.Discriminator}, id {guildUser.Id}");
                await guildUser.RemoveRoleAsync(role);
            } else
            {
                Console.WriteLine($"Added role {role.Name} to user {guildUser.Username + "#" + guildUser.Discriminator}, id {guildUser.Id}");
                await guildUser.AddRoleAsync(role);
            }

            await SendConfirmation();
        }

        //this is sad code but eh well.
        [Command("programming")]
        public async Task Programmer() => await ToggleRole(minorRoles[Minors.Programming]);

        [Command("graphics")]
        public async Task Graphics() => await ToggleRole(minorRoles[Minors.Graphics]);

        [Command("design")]
        public async Task Design() => await ToggleRole(minorRoles[Minors.Design]);

        [Command("pm")]
        public async Task PM() => await ToggleRole(minorRoles[Minors.PM]);


        [Command("newcomer")]
        public async Task ZerothYear() => await ToggleRole(yearRoles[Years.Newcomer]);

        [Command("1st")]
        public async Task FirstYear() => await ToggleRole(yearRoles[Years.First]);

        [Command("2nd")]
        public async Task SecondYear() => await ToggleRole(yearRoles[Years.Second]);

        [Command("3rd")]
        public async Task ThirdYear() => await ToggleRole(yearRoles[Years.Third]);

        [Command("4th")]
        public async Task Masters() => await ToggleRole(yearRoles[Years.Masters]);

        [Command("alumni")]
        public async Task Alumni() => await ToggleRole(yearRoles[Years.Alumni]);


        [Command("help")]
        public async Task Help() 
        {
            await ReplyAsync(
                "Frogbot to the rescue!\n" +
                "All of my commands are preceded by \"!\"\n" +
                "Role commands are programming, graphics, design, pm, newcomer, 1st, 2nd, 3rd, 4th and alumni\n" +
                "If you can manage messages, cleanup <Amount> removes Amount of my messages\n" +
                "So far, that's about it!"
                );
        }

        [Command("cleanup")]
        public async Task Cleanup(int amount)
        {
            if (amount <= 0) amount = 100;

            var user = Context.User as IGuildUser;
            if (user.GuildPermissions.ManageMessages) 
            {
                var requestOptions = new RequestOptions();
                var reason = $"Bot cleanup asked by {user.Nickname}#{user.Discriminator}";
                requestOptions.AuditLogReason = reason;
                Console.WriteLine(reason);
                var messagesRequest = Context.Channel.GetMessagesAsync(amount, options: requestOptions);
                await foreach(var messages in messagesRequest) 
                {
                    foreach(var message in messages)
                    {
                        if (message.Content.StartsWith('!') || message.Author.IsBot)
                        {
                            await Context.Channel.DeleteMessageAsync(message);
                            await Task.Delay(100);
                        }
                    }
                }
            }
        }

        [Command("flushlog")]
        public async Task FlushLog() 
        {
            var user = Context.User as IGuildUser;
            if (user.GuildPermissions.Administrator) 
            {

                Console.Clear();

                await SendConfirmation();
            } else 
            {
                await SendDenial();
            }
        }
    }
}
