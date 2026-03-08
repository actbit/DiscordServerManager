using Discord;
using Discord.Interactions;
using DiscordServerManager.Data;
using DiscordServerManager.Preconditions;
using DiscordServerManager.Services;

namespace DiscordServerManager.Interactions
{
    [Group("admin", "管理者が使用するコマンドです")]
    [RequireAdmin]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        public ServerService ServerService { get; set; } = null!;

        [Group("channel-makeable", "作成可能なチャンネルの設定をします")]
        [RequireAdmin]
        public class ChannelMakeableModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            [SlashCommand("add-user", "カテゴリにチャンネル作成権限を持つユーザーを追加します")]
            public async Task AddUser(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IUser user)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var categoryData = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);
                if (categoryData == null)
                {
                    categoryData = new CategoryDataClass { CategoryID = categoryChannel.Id };
                    server.Categorys.Add(categoryData);
                }

                if (categoryData.UserIds.Contains(user.Id))
                {
                    await RespondAsync($"{user.Mention} は既に追加されています");
                    return;
                }

                categoryData.UserIds.Add(user.Id);
                ServerService.SaveServerData(server);

                await RespondAsync($"<#{categoryChannel.Id}> に {user.Mention} を追加しました");
            }

            [SlashCommand("add-role", "カテゴリにチャンネル作成権限を持つロールを追加します")]
            public async Task AddRole(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IRole role)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var categoryData = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);
                if (categoryData == null)
                {
                    categoryData = new CategoryDataClass { CategoryID = categoryChannel.Id };
                    server.Categorys.Add(categoryData);
                }

                if (categoryData.RoleIds.Contains(role.Id))
                {
                    await RespondAsync($"{role.Mention} は既に追加されています");
                    return;
                }

                categoryData.RoleIds.Add(role.Id);
                ServerService.SaveServerData(server);

                await RespondAsync($"<#{categoryChannel.Id}> に {role.Mention} を追加しました");
            }

            [SlashCommand("remove-user", "カテゴリからチャンネル作成権限を持つユーザーを削除します")]
            public async Task RemoveUser(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IUser user)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var categoryData = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);
                if (categoryData == null)
                {
                    await RespondAsync("このカテゴリは設定されていません");
                    return;
                }

                if (categoryData.UserIds.Remove(user.Id))
                {
                    // ユーザーもロールもいなくなったらカテゴリ自体を削除
                    if (categoryData.UserIds.Count == 0 && categoryData.RoleIds.Count == 0)
                    {
                        server.Categorys.Remove(categoryData);
                    }
                    ServerService.SaveServerData(server);
                    await RespondAsync($"<#{categoryChannel.Id}> から {user.Mention} を削除しました");
                }
                else
                {
                    await RespondAsync($"{user.Mention} は登録されていません");
                }
            }

            [SlashCommand("remove-role", "カテゴリからチャンネル作成権限を持つロールを削除します")]
            public async Task RemoveRole(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IRole role)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryChannel = category as ICategoryChannel;

                if (categoryChannel == null)
                {
                    await RespondAsync("カテゴリを指定してください");
                    return;
                }

                var categoryData = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);
                if (categoryData == null)
                {
                    await RespondAsync("このカテゴリは設定されていません");
                    return;
                }

                if (categoryData.RoleIds.Remove(role.Id))
                {
                    // ユーザーもロールもいなくなったらカテゴリ自体を削除
                    if (categoryData.UserIds.Count == 0 && categoryData.RoleIds.Count == 0)
                    {
                        server.Categorys.Remove(categoryData);
                    }
                    ServerService.SaveServerData(server);
                    await RespondAsync($"<#{categoryChannel.Id}> から {role.Mention} を削除しました");
                }
                else
                {
                    await RespondAsync($"{role.Mention} は登録されていません");
                }
            }

            [SlashCommand("list", "チャンネルを作成可能なカテゴリ一覧を表示します")]
            public async Task List()
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.Categorys.Count == 0)
                {
                    await RespondAsync("ユーザーが作成可能なカテゴリは存在しません");
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

                    var users = categoryData.UserIds.Select(id => $"<@{id}>");
                    var roles = categoryData.RoleIds.Select(id => $"<@&{id}>");
                    var allMentions = string.Join(", ", users.Concat(roles));

                    if (string.IsNullOrEmpty(allMentions))
                    {
                        allMentions = "なし";
                    }

                    embed.AddField($"#{category.Name}", allMentions, false);
                }

                await RespondAsync(embeds: new Embed[] { embed.Build() });
            }

            [SlashCommand("remove", "カテゴリを完全に削除します")]
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
                    await RespondAsync($"<#{categoryChannel.Id}>はチャンネルが作成可能なカテゴリではなくなりました");
                }
                else
                {
                    await RespondAsync("このカテゴリは設定されていません");
                }
            }
        }

        [Group("manager", "ボット管理者の設定をします（サーバーオーナー専用）")]
        public class ManagerModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            public override async Task BeforeExecuteAsync(ICommandInfo command)
            {
                // サーバーオーナーのみ許可
                if (Context.Guild?.OwnerId != Context.User.Id)
                {
                    await Context.Interaction.RespondAsync("このコマンドはサーバーオーナーのみ使用できます", ephemeral: true);
                    throw new Exception("Not server owner");
                }
            }

            [SlashCommand("add-user", "ボット管理者ユーザーを追加します")]
            public async Task AddUser(IUser user)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.AdminUserIDs.Contains(user.Id))
                {
                    await RespondAsync($"{user.Mention} は既に管理者です");
                    return;
                }

                server.AdminUserIDs.Add(user.Id);
                ServerService.SaveServerData(server);

                await RespondAsync($"{user.Mention} を管理者に追加しました");
            }

            [SlashCommand("add-role", "ボット管理者ロールを追加します")]
            public async Task AddRole(IRole role)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.AdminRoleIDs.Contains(role.Id))
                {
                    await RespondAsync($"{role.Mention} は既に管理者ロールです");
                    return;
                }

                server.AdminRoleIDs.Add(role.Id);
                ServerService.SaveServerData(server);

                await RespondAsync($"{role.Mention} を管理者ロールに追加しました");
            }

            [SlashCommand("remove-user", "ボット管理者ユーザーを削除します")]
            public async Task RemoveUser(IUser user)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.AdminUserIDs.Remove(user.Id))
                {
                    ServerService.SaveServerData(server);
                    await RespondAsync($"{user.Mention} を管理者から削除しました");
                }
                else
                {
                    await RespondAsync($"{user.Mention} は管理者ではありません");
                }
            }

            [SlashCommand("remove-role", "ボット管理者ロールを削除します")]
            public async Task RemoveRole(IRole role)
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.AdminRoleIDs.Remove(role.Id))
                {
                    ServerService.SaveServerData(server);
                    await RespondAsync($"{role.Mention} を管理者ロールから削除しました");
                }
                else
                {
                    await RespondAsync($"{role.Mention} は管理者ロールではありません");
                }
            }

            [SlashCommand("list", "ボット管理者一覧を表示します")]
            public async Task List()
            {
                var server = ServerService.GetServerData(Context.Guild.Id);

                var embed = new EmbedBuilder
                {
                    Title = "ボット管理者一覧",
                    Color = Color.Purple,
                    Timestamp = DateTime.Now
                };

                var users = server.AdminUserIDs.Select(id => $"<@{id}>");
                var roles = server.AdminRoleIDs.Select(id => $"<@&{id}>");

                var userList = string.Join("\n", users);
                var roleList = string.Join("\n", roles);

                embed.AddField("管理者ユーザー", string.IsNullOrEmpty(userList) ? "なし" : userList, false);
                embed.AddField("管理者ロール", string.IsNullOrEmpty(roleList) ? "なし" : roleList, false);

                await RespondAsync(embeds: new Embed[] { embed.Build() });
            }
        }
    }
}
