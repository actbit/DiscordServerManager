using Discord;
using Discord.Interactions;
using DiscordServerManager.Services;

namespace DiscordServerManager.Preconditions
{
    /// <summary>
    /// サーバーオーナーまたはボット管理者のみコマンドを実行できるようにする属性
    /// </summary>
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context,
            ICommandInfo commandInfo,
            IServiceProvider services)
        {
            // ギルド内でのみ使用可能
            if (context.Guild == null)
            {
                return Task.FromResult(
                    PreconditionResult.FromError("このコマンドはサーバー内でのみ使用できます"));
            }

            // ユーザー情報の取得
            if (context.User is not IGuildUser user)
            {
                return Task.FromResult(
                    PreconditionResult.FromError("ユーザー情報を取得できません"));
            }

            // サーバーオーナーは常に許可
            if (context.Guild.OwnerId == user.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            // ServerServiceの取得
            var serverService = services.GetService(typeof(ServerService)) as ServerService;
            if (serverService == null)
            {
                return Task.FromResult(
                    PreconditionResult.FromError("サーバー設定を取得できません"));
            }

            // 管理者チェック
            var serverData = serverService.GetServerData(context.Guild.Id);
            if (IsAdmin(user, serverData))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(
                PreconditionResult.FromError("このコマンドを実行する権限がありません"));
        }

        private bool IsAdmin(IGuildUser user, Data.ServerDataClass serverData)
        {
            // ユーザーIDチェック
            if (serverData.AdminUserIDs.Contains(user.Id))
                return true;

            // ロールIDチェック
            if (serverData.AdminRoleIDs.Any(roleId => user.RoleIds.Contains(roleId)))
                return true;

            return false;
        }
    }
}
