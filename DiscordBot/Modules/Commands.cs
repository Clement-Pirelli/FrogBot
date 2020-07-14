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
            Programming, Graphics, Design, PM
        }

        private static readonly Dictionary<Minors, string> minorRoles = new Dictionary<Minors, string>()
        {
            { Minors.Programming, "Programming" },
            { Minors.Graphics, "Graphics" },
            { Minors.Design, "Design" },
            { Minors.PM, "Project Management" }
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

        private static readonly Dictionary<Years, string> yearRoles = new Dictionary<Years, string>()
        {
           {Years.Newcomer,"0 Year"},
           {Years.First,"1st Year"},
           {Years.Second,"2nd Year"},
           {Years.Third,"3rd Year"},
           {Years.Masters,"4th Year - Masters"},
           {Years.Alumni,"Alumni"},
        };


        private bool IsValidRoleRequest(IGuildUser user, SocketRole role)
        {
            bool yearRole = yearRoles.ContainsValue(role.Name);
            bool minorRole = minorRoles.ContainsValue(role.Name);
            
            if (!yearRole && !minorRole) return true;

            foreach (var id in user.RoleIds)
            {
                if (id == role.Id) continue;

                var roleName = Context.Guild.GetRole(id).Name;

                if (yearRole)
                {
                    if (yearRoles.ContainsValue(roleName))
                        return false;
                }
                else //minor role
                {
                    //masters students dont have minors
                    if(roleName == yearRoles[Years.Masters] || minorRoles.ContainsValue(roleName))
                        return false;
                }
            }

            return true;
        }

        private async Task SendConfirmation()
        {
            if (Emote.TryParse("<:frogthumbsup:722037848890671105>", out var emote))
            {
                await Context.Message.AddReactionAsync(emote);
            }
            else
            {
                await Context.Message.AddReactionAsync(new Emoji("👍"));
            }
        }
        private async Task SendDenial() 
        {
            if (Emote.TryParse("<:frogthumbsdown:722234808327209010>", out var emote))
            {
                await Context.Message.AddReactionAsync(emote);
            }
            else
            {
                await Context.Message.AddReactionAsync(new Emoji("👎"));
            }
        }

        private async Task ToggleRole(string roleName)
        {
            var user = Context.User as IGuildUser;
            var ghostRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Ghosts");
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            
            //check if the request is valid
            if (ghostRole == null || role == null || !IsValidRoleRequest(user, role)) 
            {
                await SendDenial();
                return;
            }
            
            //remove ghost role if the user has it
            if (user.RoleIds.Contains(ghostRole.Id)) await user.RemoveRoleAsync(ghostRole);

            if (user.RoleIds.Contains(role.Id))
            {
                Console.WriteLine($"Removed role {roleName} from user {user.Username + "#" + user.Discriminator}, id {user.Id}");
                await user.RemoveRoleAsync(role);
            } else
            {
                Console.WriteLine($"Added role {roleName} to user {user.Username + "#" + user.Discriminator}, id {user.Id}");
                await user.AddRoleAsync(role);
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
                "If you can manage message, cleanup <Amount> removes Amount of my messages\n" +
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
                requestOptions.AuditLogReason = $"Bot cleanup asked by {user.Nickname}#{user.Discriminator}";
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
