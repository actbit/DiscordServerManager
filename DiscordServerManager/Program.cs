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
            var basePath = Directory.GetParent(typeof(Program).Assembly.Location)!.FullName;
            var serversDirectory = Directory.CreateDirectory(Path.Combine(basePath, "Servers")).FullName;

            try
            {
                Tokens.LoadFromFile(basePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 設定ファイルの読み込みに失敗しました: {ex.Message}");
                Console.WriteLine("何かキーを押して終了...");
                Console.ReadKey();
                return;
            }

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

        public Program(string basePath, string serversDirectory)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            });

            _interactions = new InteractionService(_client.Rest);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_interactions)
                .AddSingleton(new ServerService(serversDirectory))
                .BuildServiceProvider();

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
            foreach (var guild in _client.Guilds)
            {
                try
                {
                    await _interactions.RegisterCommandsToGuildAsync(guild.Id);
                    Console.WriteLine($"コマンドを登録: {guild.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"登録エラー ({guild.Name}): {ex.Message}");
                }
            }
        }

        private async Task InteractionCreated(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                var result = await _interactions.ExecuteCommandAsync(ctx, _services);

                if (!result.IsSuccess)
                {
                    var message = result.Error switch
                    {
                        InteractionCommandError.UnmetPrecondition => result.ErrorReason,
                        InteractionCommandError.BadArgs => "引数が正しくありません",
                        InteractionCommandError.ParseFailed => "コマンドの解析に失敗しました",
                        _ => $"エラー: {result.ErrorReason}"
                    };

                    Console.WriteLine($"[ERROR] {result.Error}: {result.ErrorReason}");

                    if (!arg.HasResponded)
                    {
                        await arg.RespondAsync(message, ephemeral: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Interaction例外: {ex}");

                if (!arg.HasResponded)
                {
                    await arg.RespondAsync("予期しないエラーが発生しました", ephemeral: true);
                }
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
