using Discord;
using Discord.Interactions;
using DiscordServerManager.Data;
using DiscordServerManager.Services;

namespace DiscordServerManager.Interactions
{
    [Group("admin", "管理者が使用するコマンドです")]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        public ServerService ServerService { get; set; } = null!;

        [Group("channel-makeable", "作成可能なチャンネルの設定をします")]
        public class ChannelMakeableModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            [SlashCommand("set", "チャンネルを作成可能なカテゴリを指定します")]
            public async Task Set(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IMentionable user)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var categoryData = new CategoryDataClass { CategoryID = categoryChannel.Id };

                var index = server.Categorys.FindIndex(a => a.CategoryID == categoryChannel.Id);
                if (index != -1)
                {
                    server.Categorys.RemoveAt(index);
                }
                server.Categorys.Add(categoryData);

                if (user is IRole role)
                {
                    categoryData.RoleId = role.Id;
                    await RespondAsync($"<#{categoryChannel.Id}> は **@{role.Name}** によってチャンネルの作成が可能なカテゴリになりました");
                }
                else if (user is IUser socketUser)
                {
                    categoryData.UserId = socketUser.Id;
                    await RespondAsync($"<#{categoryChannel.Id}> は **@{socketUser.Username}** によってチャンネルの作成が可能なカテゴリになりました");
                }
                else
                {
                    await RespondAsync("ユーザまたはロールを指定してください");
                    return;
                }

                ServerService.SaveServerData(server);
            }

            [SlashCommand("get", "チャンネルを作成可能カテゴリを取得します")]
            public async Task Get()
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.Categorys.Count == 0)
                {
                    await RespondAsync("ユーザが作成可能なカテゴリは存在しません");
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Title = "ユーザーが作成可能なカテゴリ",
                    Color = Color.Orange,
                    Timestamp = DateTime.Now
                };

                foreach (var categoryData in server.Categorys)
                {
                    var category = Context.Guild.GetChannel(categoryData.CategoryID);
                    if (category == null) continue;

                    var addword = categoryData.UserId != null
                        ? $"<@!{categoryData.UserId}>"
                        : $"<@&{categoryData.RoleId}>";

                    embed.AddField($"#{category.Name}", addword, false);
                }

                await RespondAsync(embeds: new Embed[] { embed.Build() });
            }

            [SlashCommand("remove", "チャンネルを作成可能なカテゴリを削除")]
            public async Task Remove(
                [ChannelTypes(ChannelType.Category)] IChannel category)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var index = server.Categorys.FindIndex(a => a.CategoryID == categoryChannel.Id);
                if (index != -1)
                {
                    server.Categorys.RemoveAt(index);
                    ServerService.SaveServerData(server);
                }

                await RespondAsync($"<#{categoryChannel.Id}>はチャンネルが作成可能なカテゴリではなくなりました");
            }
        }
    }
}
