using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace DiscordBot
{
    static class Utilities
    {
        private const ulong buddiesId = 441370070711533588;
        public static SocketGuild BuddiesGuild(DiscordSocketClient client) 
        {
            return client.GetGuild(buddiesId);
        }

        public static async Task ToggleRole(DiscordSocketClient client, SocketUser user, ulong roleId, SocketMessage optionalMessage = null)
        {
            try
            {
                var buddiesGuild = BuddiesGuild(client);
                if (buddiesGuild == null)
                {
                    throw new Exception("Buddies can't be found. Bailing out!");
                }

                //try to get the user
                IGuildUser guildUser = null;
                guildUser = user as IGuildUser;

                //if we're in DMs
                if (guildUser == null)
                {
                    guildUser = await GetUserFromGuild(buddiesGuild, user);
                }

                if (guildUser == null)
                {
                    var message = $"User @{user}'s GuildUser could not be retrieved for role : { buddiesGuild.GetRole(roleId).Name }";
                    await SendToModeratorChat(message, buddiesGuild);
                    await user.SendMessageAsync("I'm really sorry but something went terribly wrong in toggling your role :( Don't worry though, a moderator will see that this operation has failed and hopefully add your role manually soon. \nApologies!");
                    throw new Exception(message);
                }

                SocketRole role, ghostRole;
                ghostRole = buddiesGuild.GetRole(496632805820727298); //ghost role id
                role = buddiesGuild.GetRole(roleId);


                if (guildUser.RoleIds.Contains(role.Id))
                {
                    Console.WriteLine($"Removed role {role.Name} from user {user}, id {user.Id}");
                    await guildUser.RemoveRoleAsync(role); 
                    if (guildUser.RoleIds.Count == 2) //2 because the role we just removed + @everyone, which is a role... 
                        await guildUser.AddRoleAsync(ghostRole);
                }
                else
                {
                    //remove ghost role if the user has it
                    if (guildUser.RoleIds.Contains(ghostRole.Id)) await guildUser.RemoveRoleAsync(ghostRole);
                    Console.WriteLine($"Added role {role.Name} to user {user}, id {user.Id}");
                    await guildUser.AddRoleAsync(role);
                }

                if(optionalMessage != null) await SendConfirmation(optionalMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (optionalMessage != null) await SendDenial(optionalMessage);
            }
        }

        public static async Task SendConfirmation(IMessage message)
        {
            if (Emote.TryParse("<:frogthumbsup:733751715160915968>", out var emote))
            {
                await message.AddReactionAsync(emote);
            }
            else
            {
                await message.AddReactionAsync(new Emoji(char.ConvertFromUtf32(0x1F44D)));
            }
        }
        public static async Task SendDenial(IMessage message)
        {
            if (Emote.TryParse("<:frogthumbsdown:733751715098001509>", out var emote))
            {
                await message.AddReactionAsync(emote);
            }
            else
            {
                await message.AddReactionAsync(new Emoji(char.ConvertFromUtf32(0x1F44E)));
            }
        }

        public static async Task SendToModeratorChat(string message, SocketGuild guild)
        {
            Console.WriteLine(message);
            var moderatorChannel = guild.GetTextChannel(592090738901254145);
            await moderatorChannel.SendMessageAsync(message);
        }

        public static async Task<IGuildUser> GetUserFromGuild(SocketGuild guild, SocketUser user)
        {
            SocketGuildUser buddiesUser = guild.GetUser(user.Id);

            if (buddiesUser == null) //user isn't found in the guild
            {
                var message = $"Couldn't get GuildUser for user: @{user}";
                await SendToModeratorChat(message, guild);
                return null;
            }

            return buddiesUser;
        }

        public static List<SocketGuildUser> GetGhostsAndNoRoles(SocketGuild guild) 
        {
            return guild.Users.Where(
                user => user.Roles.Count < 0 
                || user.Roles.Any(role => role.Name == "Ghosts")
                ).ToList();
        }
    }
}
