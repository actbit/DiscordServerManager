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
                Console.WriteLine("[DEBUG] Guild is null");
                return PreconditionResult.FromError("このコマンドはサーバー内でのみ使用できます");
            }

            if (context.User is not IGuildUser user)
            {
                Console.WriteLine($"[DEBUG] User is not IGuildUser: {context.User.GetType().Name}");
                return PreconditionResult.FromError("ユーザー情報を取得できません");
            }

            Console.WriteLine($"[DEBUG] Checking: {user.Username} (Owner: {context.Guild.OwnerId == user.Id})");

            // サーバーオーナーは常に許可
            if (context.Guild.OwnerId == user.Id)
            {
                Console.WriteLine("[DEBUG] Passed: Owner");
                return PreconditionResult.FromSuccess();
            }

            var serverService = services.GetService(typeof(ServerService)) as ServerService;
            if (serverService == null)
            {
                Console.WriteLine("[DEBUG] ServerService is null, allowing");
                return PreconditionResult.FromSuccess();
            }

            var serverData = serverService.GetServerData(context.Guild.Id);

            Console.WriteLine($"[DEBUG] AdminUserIDs: [{string.Join(", ", serverData.AdminUserIDs)}]");
            Console.WriteLine($"[DEBUG] AdminRoleIDs: [{string.Join(", ", serverData.AdminRoleIDs)}]");
            Console.WriteLine($"[DEBUG] UserRoles: [{string.Join(", ", user.RoleIds)}]");

            bool isAdminUser = serverData.AdminUserIDs.Contains(user.Id);
            bool hasAdminRole = serverData.AdminRoleIDs.Any(roleId => user.RoleIds.Contains(roleId));

            Console.WriteLine($"[DEBUG] isAdminUser: {isAdminUser}, hasAdminRole: {hasAdminRole}");

            if (isAdminUser && hasAdminRole)
            {
                Console.WriteLine("[DEBUG] Passed: Admin");
                return PreconditionResult.FromSuccess();
            }

            Console.WriteLine("[DEBUG] Failed: No permission");
            return PreconditionResult.FromError("このコマンドを実行する権限がありません");
        }
    }
}
