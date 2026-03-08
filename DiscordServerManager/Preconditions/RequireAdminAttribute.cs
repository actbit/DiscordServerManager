using Discord;
using Discord.Interactions;
using DiscordServerManager.Services;

namespace DiscordServerManager.Preconditions
{
    public class RequireAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context,
            ICommandInfo commandInfo,
            IServiceProvider services)
        {
            if (context.Guild == null)
            {
                return PreconditionResult.FromError("このコマンドはサーバー内でのみ使用できます");
            }

            if (context.User is not IGuildUser user)
            {
                return PreconditionResult.FromError("ユーザー情報を取得できません");
            }

            // サーバーオーナーは常に許可
            if (context.Guild.OwnerId == user.Id)
            {
                return PreconditionResult.FromSuccess();
            }

            var serverService = services.GetService(typeof(ServerService)) as ServerService;
            if (serverService == null)
            {
                return PreconditionResult.FromError("サーバー設定を取得できません");
            }

            var serverData = serverService.GetServerData(context.Guild.Id);

            bool isAdminUser = serverData.AdminUserIDs.Contains(user.Id);
            bool hasAdminRole = serverData.AdminRoleIDs.Any(roleId => user.RoleIds.Contains(roleId));

            if (isAdminUser || hasAdminRole)
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("このコマンドを実行する権限がありません");
        }
    }
}
