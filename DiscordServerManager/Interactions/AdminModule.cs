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
        public class ChannelMakeableModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            [SlashCommand("add-user", "カテゴリにチャンネル作成権限を持つユーザーを追加します")]
            public async Task AddUser(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IUser user)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = GetOrCreateCategoryData(server, categoryChannel.Id);

                    if (categoryData.UserIds.Contains(user.Id))
                    {
                        await FollowupAsync($"{user.Mention} は既に追加されています", ephemeral: true);
                        return;
                    }

                    categoryData.UserIds.Add(user.Id);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"<#{categoryChannel.Id}> に {user.Mention} を追加しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] add-userコマンドエラー: {ex.Message}");
                    await FollowupAsync("ユーザーの追加に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("add-role", "カテゴリにチャンネル作成権限を持つロールを追加します")]
            public async Task AddRole(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IRole role)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = GetOrCreateCategoryData(server, categoryChannel.Id);

                    if (categoryData.RoleIds.Contains(role.Id))
                    {
                        await FollowupAsync($"{role.Mention} は既に追加されています", ephemeral: true);
                        return;
                    }

                    categoryData.RoleIds.Add(role.Id);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"<#{categoryChannel.Id}> に {role.Mention} を追加しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] add-roleコマンドエラー: {ex.Message}");
                    await FollowupAsync("ロールの追加に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("remove-user", "カテゴリからチャンネル作成権限を持つユーザーを削除します")]
            public async Task RemoveUser(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IUser user)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = server.Categorys.FirstOrDefault(c => c.CategoryID == categoryChannel.Id);

                    if (categoryData == null)
                    {
                        await FollowupAsync("このカテゴリは設定されていません", ephemeral: true);
                        return;
                    }

                    if (!categoryData.UserIds.Remove(user.Id))
                    {
                        await FollowupAsync($"{user.Mention} は登録されていません", ephemeral: true);
                        return;
                    }

                    CleanupEmptyCategoryData(server, categoryData);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"<#{categoryChannel.Id}> から {user.Mention} を削除しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] remove-userコマンドエラー: {ex.Message}");
                    await FollowupAsync("ユーザーの削除に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("remove-role", "カテゴリからチャンネル作成権限を持つロールを削除します")]
            public async Task RemoveRole(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IRole role)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = server.Categorys.FirstOrDefault(c => c.CategoryID == categoryChannel.Id);

                    if (categoryData == null)
                    {
                        await FollowupAsync("このカテゴリは設定されていません", ephemeral: true);
                        return;
                    }

                    if (!categoryData.RoleIds.Remove(role.Id))
                    {
                        await FollowupAsync($"{role.Mention} は登録されていません", ephemeral: true);
                        return;
                    }

                    CleanupEmptyCategoryData(server, categoryData);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"<#{categoryChannel.Id}> から {role.Mention} を削除しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] remove-roleコマンドエラー: {ex.Message}");
                    await FollowupAsync("ロールの削除に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("list", "チャンネルを作成可能なカテゴリ一覧を表示します")]
            public async Task List()
            {
                await DeferAsync();

                var server = ServerService.GetServerData(Context.Guild.Id);

                if (server.Categorys.Count == 0)
                {
                    await FollowupAsync("ユーザーが作成可能なカテゴリは存在しません", ephemeral: true);
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Title = "ユーザーが作成可能なカテゴリ",
                    Color = Color.Orange,
                    Timestamp = DateTimeOffset.Now
                };

                foreach (var categoryData in server.Categorys)
                {
                    var category = Context.Guild.GetChannel(categoryData.CategoryID);
                    if (category == null) continue;

                    var mentions = BuildMentionsList(categoryData.UserIds, categoryData.RoleIds);
                    embed.AddField($"#{category.Name}", mentions, false);
                }

                await FollowupAsync(embed: embed.Build());
            }

            [SlashCommand("get", "指定したカテゴリの権限設定を表示します")]
            public async Task Get([ChannelTypes(ChannelType.Category)] IChannel category)
            {
                await DeferAsync();

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                var server = ServerService.GetServerData(Context.Guild.Id);
                var categoryData = server.Categorys.FirstOrDefault(c => c.CategoryID == categoryChannel.Id);

                var embed = new EmbedBuilder
                {
                    Title = $"#{categoryChannel.Name} の権限設定",
                    Color = Color.Blue,
                    Timestamp = DateTimeOffset.Now
                };

                if (categoryData == null)
                {
                    embed.Description = "このカテゴリは登録されていません";
                    embed.AddField("ユーザー", "なし", false);
                    embed.AddField("ロール", "なし", false);
                }
                else
                {
                    var userMentions = BuildMentionsList(categoryData.UserIds, new List<ulong>());
                    var roleMentions = BuildMentionsList(new List<ulong>(), categoryData.RoleIds);
                    embed.AddField("ユーザー", string.IsNullOrEmpty(userMentions) ? "なし" : userMentions, false);
                    embed.AddField("ロール", string.IsNullOrEmpty(roleMentions) ? "なし" : roleMentions, false);
                }

                await FollowupAsync(embed: embed.Build());
            }

            [SlashCommand("set", "カテゴリを作成可能に設定します（権限を上書き）")]
            public async Task Set(
                [ChannelTypes(ChannelType.Category)] IChannel category,
                IUser? user = null,
                IRole? role = null)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                if (user == null && role == null)
                {
                    await FollowupAsync("ユーザーまたはロールを最低1つ指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = GetOrCreateCategoryData(server, categoryChannel.Id);

                    if (user != null)
                    {
                        categoryData.UserIds.Clear();
                        categoryData.UserIds.Add(user.Id);
                    }
                    if (role != null)
                    {
                        categoryData.RoleIds.Clear();
                        categoryData.RoleIds.Add(role.Id);
                    }

                    ServerService.SaveServerData(server);
                    await FollowupAsync($"<#{categoryChannel.Id}> を作成可能カテゴリに設定しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] setコマンドエラー: {ex.Message}");
                    await FollowupAsync("設定の保存に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("remove", "カテゴリを完全に削除します")]
            public async Task Remove([ChannelTypes(ChannelType.Category)] IChannel category)
            {
                await DeferAsync(ephemeral: true);

                var categoryChannel = ValidateCategoryChannel(category);
                if (categoryChannel == null)
                {
                    await FollowupAsync("カテゴリを指定してください", ephemeral: true);
                    return;
                }

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var categoryData = server.Categorys.FirstOrDefault(c => c.CategoryID == categoryChannel.Id);

                    if (categoryData == null)
                    {
                        await FollowupAsync("このカテゴリは設定されていません", ephemeral: true);
                        return;
                    }

                    server.Categorys.Remove(categoryData);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"<#{categoryChannel.Id}> はチャンネルが作成可能なカテゴリではなくなりました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] removeコマンドエラー: {ex.Message}");
                    await FollowupAsync("カテゴリの削除に失敗しました", ephemeral: true);
                }
            }

            // ヘルパーメソッド
            private ICategoryChannel? ValidateCategoryChannel(IChannel channel)
                => channel as ICategoryChannel;

            private CategoryDataClass GetOrCreateCategoryData(ServerDataClass server, ulong categoryId)
            {
                var categoryData = server.Categorys.FirstOrDefault(c => c.CategoryID == categoryId);
                if (categoryData == null)
                {
                    categoryData = new CategoryDataClass { CategoryID = categoryId };
                    server.Categorys.Add(categoryData);
                }
                return categoryData;
            }

            private void CleanupEmptyCategoryData(ServerDataClass server, CategoryDataClass categoryData)
            {
                if (categoryData.UserIds.Count == 0 && categoryData.RoleIds.Count == 0)
                {
                    server.Categorys.Remove(categoryData);
                }
            }

            private string BuildMentionsList(List<ulong> userIds, List<ulong> roleIds)
            {
                var users = userIds.Select(id => $"<@{id}>");
                var roles = roleIds.Select(id => $"<@&{id}>");
                var allMentions = string.Join(", ", users.Concat(roles));
                return string.IsNullOrEmpty(allMentions) ? "なし" : allMentions;
            }
        }

        [Group("manager", "ボット管理者の設定をします（サーバーオーナー専用）")]
        [DiscordServerManager.Preconditions.RequireOwner]
        public class ManagerModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            [SlashCommand("add-user", "ボット管理者ユーザーを追加します")]
            public async Task AddUser(IUser user)
            {
                await DeferAsync(ephemeral: true);

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);

                    if (server.AdminUserIDs.Contains(user.Id))
                    {
                        await FollowupAsync($"{user.Mention} は既に管理者です", ephemeral: true);
                        return;
                    }

                    server.AdminUserIDs.Add(user.Id);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"{user.Mention} を管理者に追加しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] manager add-userコマンドエラー: {ex.Message}");
                    await FollowupAsync("管理者ユーザーの追加に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("add-role", "ボット管理者ロールを追加します")]
            public async Task AddRole(IRole role)
            {
                await DeferAsync(ephemeral: true);

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);

                    if (server.AdminRoleIDs.Contains(role.Id))
                    {
                        await FollowupAsync($"{role.Mention} は既に管理者ロールです", ephemeral: true);
                        return;
                    }

                    server.AdminRoleIDs.Add(role.Id);
                    ServerService.SaveServerData(server);

                    await FollowupAsync($"{role.Mention} を管理者ロールに追加しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] manager add-roleコマンドエラー: {ex.Message}");
                    await FollowupAsync("管理者ロールの追加に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("remove-user", "ボット管理者ユーザーを削除します")]
            public async Task RemoveUser(IUser user)
            {
                await DeferAsync(ephemeral: true);

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);

                    if (!server.AdminUserIDs.Remove(user.Id))
                    {
                        await FollowupAsync($"{user.Mention} は管理者ではありません", ephemeral: true);
                        return;
                    }

                    ServerService.SaveServerData(server);
                    await FollowupAsync($"{user.Mention} を管理者から削除しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] manager remove-userコマンドエラー: {ex.Message}");
                    await FollowupAsync("管理者ユーザーの削除に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("remove-role", "ボット管理者ロールを削除します")]
            public async Task RemoveRole(IRole role)
            {
                await DeferAsync(ephemeral: true);

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);

                    if (!server.AdminRoleIDs.Remove(role.Id))
                    {
                        await FollowupAsync($"{role.Mention} は管理者ロールではありません", ephemeral: true);
                        return;
                    }

                    ServerService.SaveServerData(server);
                    await FollowupAsync($"{role.Mention} を管理者ロールから削除しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] manager remove-roleコマンドエラー: {ex.Message}");
                    await FollowupAsync("管理者ロールの削除に失敗しました", ephemeral: true);
                }
            }

            [SlashCommand("list", "ボット管理者一覧を表示します")]
            public async Task List()
            {
                await DeferAsync();

                var server = ServerService.GetServerData(Context.Guild.Id);

                var embed = new EmbedBuilder
                {
                    Title = "ボット管理者一覧",
                    Color = Color.Purple,
                    Timestamp = DateTimeOffset.Now
                };

                var userList = BuildUserMentions(server.AdminUserIDs);
                var roleList = BuildRoleMentions(server.AdminRoleIDs);

                embed.AddField("管理者ユーザー", userList, false);
                embed.AddField("管理者ロール", roleList, false);

                await FollowupAsync(embed: embed.Build());
            }

            // ヘルパーメソッド
            private string BuildUserMentions(List<ulong> userIds)
            {
                if (userIds.Count == 0) return "なし";
                return string.Join("\n", userIds.Select(id => $"<@{id}>"));
            }

            private string BuildRoleMentions(List<ulong> roleIds)
            {
                if (roleIds.Count == 0) return "なし";
                return string.Join("\n", roleIds.Select(id => $"<@&{id}>"));
            }
        }
    }
}
