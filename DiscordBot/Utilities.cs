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

namespace DiscordBot
{
    static class Utilities
    {

        public static bool IsValidRoleRequest(IGuildUser user, SocketRole role)
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
                    if (id == Program.yearRoles[Years.Masters] ||
                       id == Program.yearRoles[Years.MastersSecond] ||
                       Program.minorRoles.Data.ContainsValue(id))
                        return false;
                }
            }

            return true;
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
    }
}
