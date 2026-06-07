using Discord;
using Discord.Interactions;
using DiscordServerManager.Data;
using DiscordServerManager.Services;

namespace DiscordServerManager.Interactions
{
    [Group("user", "一般のユーザーが使用するコマンドです")]
    public class UserModule : InteractionModuleBase<SocketInteractionContext>
    {
        public ServerService ServerService { get; set; } = null!;

        [Group("channel", "チャンネルに関するコマンドです")]
        public class ChannelModule : InteractionModuleBase<SocketInteractionContext>
        {
            public ServerService ServerService { get; set; } = null!;

            [SlashCommand("make", "チャンネルを作成します")]
            public async Task Make(
                string name,
                [ChannelTypes(ChannelType.Category)] IChannel? category = null)
            {
                await DeferAsync(ephemeral: true);

                try
                {
                    var server = ServerService.GetServerData(Context.Guild.Id);
                    var guildUser = Context.User as IGuildUser;

                    if (guildUser == null)
                    {
                        await FollowupAsync("ユーザー情報を取得できませんでした", ephemeral: true);
                        return;
                    }

                    // カテゴリデータを取得
                    var categoryData = GetCategoryData(server, category);

                    if (categoryData == null)
                    {
                        await FollowupAsync("作成することを許可されていないカテゴリです", ephemeral: true);
                        return;
                    }

                    // 権限チェック
                    if (!HasPermission(categoryData, guildUser))
                    {
                        await FollowupAsync("実行権限がありません", ephemeral: true);
                        return;
                    }

                    // チャンネル作成
                    await CreateChannelAsync(name, categoryData.CategoryID, guildUser);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] makeコマンドエラー: {ex.Message}");
                    try
                    {
                        await FollowupAsync("チャンネルの作成に失敗しました", ephemeral: true);
                    }
                    catch
                    {
                        // 応答に失敗しても無視（CreateChannelAsync内で既に応答済みの場合等）
                    }
                }
            }

            // ヘルパーメソッド
            private CategoryDataClass? GetCategoryData(ServerDataClass server, IChannel? category)
            {
                // 指定されたカテゴリを優先
                if (category is ICategoryChannel categoryChannel)
                {
                    return server.Categorys.FirstOrDefault(c => c.CategoryID == categoryChannel.Id);
                }

                // 現在のチャンネルのカテゴリを使用
                if (Context.Channel is ITextChannel textChannel && textChannel.CategoryId.HasValue)
                {
                    return server.Categorys.FirstOrDefault(c => c.CategoryID == textChannel.CategoryId.Value);
                }

                return null;
            }

            private bool HasPermission(CategoryDataClass categoryData, IGuildUser user)
            {
                // ユーザーIDチェック
                if (categoryData.UserIds.Contains(user.Id))
                    return true;

                // ロールIDチェック
                if (categoryData.RoleIds.Any(roleId => user.RoleIds.Contains(roleId)))
                    return true;

                return false;
            }

            private async Task CreateChannelAsync(string name, ulong categoryId, IGuildUser creator)
            {
                ITextChannel? newChannel = null;

                try
                {
                    // チャンネル作成
                    newChannel = await Context.Guild.CreateTextChannelAsync(
                        name,
                        props => props.CategoryId = categoryId);

                    // 作成者に管理権限を付与
                    var permissions = new OverwritePermissions(
                        manageChannel: PermValue.Allow,
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        manageMessages: PermValue.Allow,
                        embedLinks: PermValue.Allow,
                        attachFiles: PermValue.Allow,
                        readMessageHistory: PermValue.Allow,
                        manageRoles: PermValue.Allow,
                        manageThreads: PermValue.Allow,
                        usePublicThreads: PermValue.Allow,
                        usePrivateThreads: PermValue.Allow,
                        createPrivateThreads: PermValue.Allow
                    );

                    await newChannel.AddPermissionOverwriteAsync(creator, permissions);

                    // 成功メッセージ
                    await FollowupAsync($"<#{newChannel.Id}> を作成しました");
                    await newChannel.SendMessageAsync($"{creator.Mention} が管理するチャンネルです");
                }
                catch (Exception ex)
                {
                    // エラー時はチャンネルを削除
                    if (newChannel != null)
                    {
                        try
                        {
                            await newChannel.DeleteAsync();
                        }
                        catch
                        {
                            // 削除に失敗しても無視
                        }
                    }

                    await FollowupAsync($"チャンネルの作成に失敗しました: {ex.Message}", ephemeral: true);
                }
            }
        }
    }
}
