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
        private readonly string _serversDirectory;

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
            _serversDirectory = serversDirectory;

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All,
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

            // ギルドコマンドとして登録（即座に反映）
            var guild = _client.GetGuild(667884699347058701);
            if (guild != null)
            {
                await _interactions.RegisterCommandsToGuildAsync(guild.Id);
                Console.WriteLine($"コマンドをギルド {guild.Id} に登録しました");
            }
            else
            {
                // ギルドが見つからない場合はグローバルコマンドとして登録
                await _interactions.RegisterCommandsGloballyAsync();
                Console.WriteLine("コマンドをグローバルに登録しました（反映まで最大1時間）");
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
                    Console.WriteLine($"[ERROR] コマンド実行エラー: {result.ErrorReason}");

                    if (!arg.HasResponded)
                    {
                        await arg.RespondAsync($"エラーが発生しました: {result.ErrorReason}", ephemeral: true);
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
