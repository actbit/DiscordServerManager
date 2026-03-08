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
                var server = ServerService.GetServerData(Context.Guild.Id);
                var guser = Context.User as IGuildUser;

                CategoryDataClass? categorydata = null;

                // 指定されたカテゴリまたは現在のチャンネルのカテゴリを確認
                if (category is ICategoryChannel categoryChannel)
                {
                    categorydata = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);
                }
                else if (Context.Channel is ITextChannel textChannel && textChannel.CategoryId.HasValue)
                {
                    categorydata = server.Categorys.FirstOrDefault(a => a.CategoryID == textChannel.CategoryId.Value);
                }

                if (categorydata == null)
                {
                    await RespondAsync("作成することを許可されていないカテゴリです");
                    return;
                }

                // 権限チェック
                bool hasPermission = categorydata.UserId == Context.User.Id ||
                    (guser != null && categorydata.RoleId != null && guser.RoleIds.Contains((ulong)categorydata.RoleId));

                if (!hasPermission)
                {
                    await RespondAsync("実行権限がありません");
                    return;
                }

                try
                {
                    var newChannel = await Context.Guild.CreateTextChannelAsync(name,
                        prop => prop.CategoryId = categorydata.CategoryID);

                    await RespondAsync($"<#{newChannel.Id}>を作成しました。");

                    await newChannel.SendMessageAsync($"{Context.User.Mention}が管理するチャンネルです");

                    var overwrite = new OverwritePermissions(
                        createInstantInvite: PermValue.Inherit,
                        manageChannel: PermValue.Allow,
                        viewChannel: PermValue.Allow,
                        sendMessages: PermValue.Allow,
                        sendTTSMessages: PermValue.Allow,
                        manageMessages: PermValue.Allow,
                        embedLinks: PermValue.Allow,
                        attachFiles: PermValue.Allow,
                        readMessageHistory: PermValue.Allow,
                        mentionEveryone: PermValue.Inherit,
                        useExternalEmojis: PermValue.Allow,
                        manageRoles: PermValue.Allow,
                        manageThreads: PermValue.Allow,
                        createPrivateThreads: PermValue.Allow,
                        usePrivateThreads: PermValue.Allow,
                        usePublicThreads: PermValue.Allow
                    );

                    await newChannel.AddPermissionOverwriteAsync(Context.User, overwrite);
                }
                catch (Exception e)
                {
                    await FollowupAsync(e.Message, ephemeral: true);
                }
            }
        }
    }
}
