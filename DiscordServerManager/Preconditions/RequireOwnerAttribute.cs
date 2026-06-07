using Discord;
using Discord.Interactions;

namespace DiscordServerManager.Preconditions
{
    public class RequireOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(
            IInteractionContext context,
            ICommandInfo commandInfo,
            IServiceProvider services)
        {
            if (context.Guild == null)
            {
                return Task.FromResult(PreconditionResult.FromError("このコマンドはサーバー内でのみ使用できます"));
            }

            if (context.Guild.OwnerId == context.User.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError("このコマンドはサーバーオーナーのみ使用できます"));
        }
    }
}
