using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordServerManager.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordServerManager
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        static async Task Main(string[] args)
        {
            var basePath = AppContext.BaseDirectory;
            var serversDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Servers")).FullName;

            Console.WriteLine($"[Info] BasePath: {basePath}");
            Console.WriteLine($"[Info] ServersDirectory: {serversDirectory}");

            // 設定ファイルの読み込み
            if (!LoadConfiguration(basePath))
                return;

            try
            {
                await new Program(basePath, serversDirectory).MainAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.WriteLine("何かキーを押して終了...");
                Console.ReadKey();
            }
        }

        private static bool LoadConfiguration(string basePath)
        {
            try
            {
                Console.WriteLine("[Debug] Loading configuration...");
                Tokens.LoadFromFile(basePath);
                Console.WriteLine("[Debug] Token loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 設定ファイルの読み込みに失敗しました: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                Console.WriteLine("何かキーを押して終了...");
                Console.ReadKey();
                return false;
            }
        }

        public Program(string basePath, string serversDirectory)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            });

            _interactions = new InteractionService(_client);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_interactions)
                .AddSingleton(new ServerService(serversDirectory))
                .BuildServiceProvider();

            // イベントハンドラーの登録
            _client.Log += Log;
            _interactions.Log += Log;
            _client.Ready += Ready;
            _client.InteractionCreated += InteractionCreated;
        }

        private static Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }

        private async Task Ready()
        {
            Console.WriteLine($"{_client.CurrentUser} is Running!!");

            // モジュールを登録
            await _interactions.AddModulesAsync(typeof(Program).Assembly, _services);

            // 参加している全ギルドにコマンド登録
            await RegisterCommandsToGuilds();

            // データクリーンアップ
            await CleanupServerDataAsync();
        }

        private async Task RegisterCommandsToGuilds()
        {
            try
            {
                // まず全球に登録
                await _interactions.RegisterCommandsGloballyAsync();
                Console.WriteLine("[Command] コマンドを全球に登録しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 全球コマンド登録エラー: {ex.Message}");
            }

            // 既存のguildにも登録（互換性のため）
            foreach (var guild in _client.Guilds)
            {
                try
                {
                    await _interactions.RegisterCommandsToGuildAsync(guild.Id);
                    Console.WriteLine($"[Command] コマンドをguildに登録: {guild.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] コマンド登録エラー ({guild.Name}): {ex.Message}");
                }
            }
        }

        private async Task CleanupServerDataAsync()
        {
            var serverService = _services.GetService<ServerService>();
            if (serverService == null) return;

            foreach (var guild in _client.Guilds)
            {
                try
                {
                    await serverService.CleanupServerDataAsync(guild);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] クリーンアップエラー ({guild.Name}): {ex.Message}");
                }
            }
        }

        private async Task InteractionCreated(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                var result = await _interactions.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                {
                    await HandleCommandError(interaction, result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Interaction例外: {ex}");
                await RespondWithError(interaction, "予期しないエラーが発生しました");
            }
        }

        private async Task HandleCommandError(SocketInteraction interaction, IResult result)
        {
            var message = result.Error switch
            {
                InteractionCommandError.UnmetPrecondition => result.ErrorReason,
                InteractionCommandError.BadArgs => "引数が正しくありません",
                InteractionCommandError.ParseFailed => "コマンドの解析に失敗しました",
                InteractionCommandError.Exception => "コマンドの実行中にエラーが発生しました",
                InteractionCommandError.Unsuccessful => "コマンドの実行に失敗しました",
                _ => $"エラー: {result.ErrorReason}"
            };

            Console.WriteLine($"[ERROR] {result.Error}: {result.ErrorReason}");
            await RespondWithError(interaction, message);
        }

        private async Task RespondWithError(SocketInteraction interaction, string message)
        {
            try
            {
                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync(message, ephemeral: true);
                }
                else
                {
                    await interaction.FollowupAsync(message, ephemeral: true);
                }
            }
            catch
            {
                // 応答に失敗しても無視（インタラクション期限切れ等）
            }
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, Tokens.Token);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }
    }
}
