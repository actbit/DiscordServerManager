using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using DiscordServerManager.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive;
using System.Text;

namespace DiscordServerManager
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        static string BasePath = "";
        static string ServersDirectory="";
        static void Main(string[] args)
        {
            BasePath = Directory.GetParent(typeof(Program).Assembly.Location).FullName;
            ServersDirectory=Directory.CreateDirectory(Path.Combine(BasePath, "Servers")).FullName;
            Tokens.LoadFromFile(BasePath);
            new Program().MainAsync().GetAwaiter().GetResult();

        }
        public Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All });
            _client.Log += _client_Log;
            _client.Ready += _client_Ready; ;
            _client.MessageReceived += _client_MessageReceived; ;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted; ;
            _client.UserCommandExecuted += _client_UserCommandExecuted; ;
            _client.ModalSubmitted += _client_ModalSubmitted; ;

        }

        private async Task _client_ModalSubmitted(SocketModal arg)
        {
            
        }

        private async Task _client_UserCommandExecuted(SocketUserCommand arg)
        {
            
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            SocketGuildChannel? socketGuildChannel = arg.Channel as SocketGuildChannel;
            if (socketGuildChannel != null)
            {
                string text = "";
                ServerDataClass server = new ServerDataClass();
                var path = Path.Combine(ServersDirectory, socketGuildChannel.Guild.Id + ".xml");
                if (File.Exists(path))
                {
                    text = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    server = XMLClass.LoadFromFile<ServerDataClass>(text);
                }
                else
                {
                    server = new ServerDataClass() { ServerID = socketGuildChannel.Guild.Id};
                }
                var sub1 = arg.Data.Options;
                var guser = arg.User as IGuildUser;

                if (arg.Data.Name == "admin")
                {
                    bool isAddminRole = false;
                    if (guser != null)
                    {
                        for (int i = 0; i < server.AdminRoleIDs.Count; i++)
                        {
                            if (guser.RoleIds.Contains(server.AdminRoleIDs[i]))
                            {
                                isAddminRole = true;
                                break;
                            }
                        }

                    }
                    if (socketGuildChannel.Guild.OwnerId == arg.User.Id || (server.AdminUserIDs.Contains(arg.Id) && isAddminRole))
                    {
                        var dataOption = sub1.FirstOrDefault();
                        if (dataOption != null)
                        {
                            if (dataOption.Name == "channel-makeable")
                            {
                                var type = dataOption.Options.FirstOrDefault();

                                if (type != null)
                                {
                                    if (type.Name == "set")
                                    {
                                        SocketCategoryChannel? categoryChannel = null;
                                        SocketRole? socketRole = null;
                                        SocketUser? socketUser = null;
                                        foreach (var option in type.Options)
                                        {
                                            if (option.Name == "category")
                                            {
                                                categoryChannel = option.Value as SocketCategoryChannel;

                                            }
                                            else if (option.Name == "user")
                                            {
                                                socketRole = option.Value as SocketRole;
                                                socketUser = option.Value as SocketUser;
                                                if (socketUser == null && socketRole == null)
                                                {
                                                    await arg.RespondAsync("ユーザを指定してください");
                                                    return;
                                                }
                                            }
                                        }
                                        if (categoryChannel == null)
                                        {
                                            await arg.RespondAsync("カテゴリを指定してください");
                                            return;
                                        }
                                        else
                                        {
                                            CategoryDataClass categoryDataClass = new CategoryDataClass() { CategoryID = categoryChannel.Id };

                                            var c = server.Categorys.FindIndex(a => a.CategoryID == categoryChannel.Id);
                                            if (c != -1)
                                            {
                                                server.Categorys.RemoveAt(c);
                                            }
                                            server.Categorys.Add(categoryDataClass);

                                            if (socketRole != null)
                                            {
                                                categoryDataClass.RoleId = socketRole.Id;
                                                await arg.RespondAsync("<#" + categoryChannel.Id + "> は**@" + socketRole.Name + "**によってチャンネルの作成が可能なカテゴリになりました");

                                            }
                                            if (socketUser != null)
                                            {
                                                categoryDataClass.UserId = socketUser.Id;
                                                await arg.RespondAsync("<#" + categoryChannel.Id + "> は**@" + socketUser.Username + "**によってチャンネルの作成が可能なカテゴリになりました");

                                            }
                                        }

                                    }
                                    else if (type.Name == "get")
                                    {
                                        if (server.Categorys.Count == 0)
                                        {
                                            await arg.RespondAsync("ユーザが作成可能なカテゴリは存在しません");
                                            return;
                                        }
                                        var embed = new EmbedBuilder() { Title = "ユーザーが作成可能なカテゴリ" };
                                        embed.Color = Color.Orange;
                                        embed.Timestamp = DateTime.Now;
                                        int i = 0;
                                        foreach (var categoryData in server.Categorys)
                                        {
                                            i++;
                                            var category = (SocketCategoryChannel)await _client.GetChannelAsync(categoryData.CategoryID);
                                            var addword ="";
                                            if (categoryData.UserId != null)
                                            {
                                                addword = "<@!" + categoryData.UserId + ">";
                                            }
                                            else
                                            {
                                                addword = "<@&" + categoryData.RoleId+ ">";

                                            }
                                            embed.AddField("#"+category.Name, addword, false);
                                        }
                                        await arg.RespondAsync(embeds: new Embed[] { embed.Build() });
                                    }
                                    else if (type.Name == "remove")
                                    {
                                        SocketCategoryChannel? categoryChannel = null;

                                        foreach (var option in type.Options)
                                        {
                                            if (option.Name == "category")
                                            {
                                                categoryChannel = option.Value as SocketCategoryChannel;
                                            }
                                        }
                                        if (categoryChannel == null)
                                        {
                                            await arg.RespondAsync("カテゴリを指定してください");
                                            return;
                                        }
                                        else
                                        {
                                            CategoryDataClass categoryDataClass = new CategoryDataClass() { CategoryID = categoryChannel.Id };

                                            var c = server.Categorys.FindIndex(a => a.CategoryID == categoryChannel.Id);
                                            if (c != -1)
                                            {
                                                server.Categorys.RemoveAt(c);
                                            }
                                            await arg.RespondAsync("<#" + categoryChannel.Id + ">はチャンネルが作成可能なカテゴリではなくなりました");
                                        }
                                    }
                                    var save = XMLClass.SaveToFile(server);
                                    File.WriteAllText(path, save, Encoding.UTF8);
                                }
                                return;

                            }
                        }
                        else
                        {

                        }
                    }

                }
                else if (arg.Data.Name == "user")
                {
                    var option = sub1.FirstOrDefault();
                    if (option != null && option.Name == "channel")
                    {
                        var option2 = option.Options.FirstOrDefault();
                        if (option2 != null && option2.Name == "make")
                        {
                            SocketCategoryChannel? categoryChannel = null;
                            string? name = null;
                            foreach (var op in option2.Options)
                            {
                                if (op.Name == "category")
                                {
                                    categoryChannel = op.Value as SocketCategoryChannel;
                                }
                                else if (op.Name == "name")
                                {

                                    name = op.Value as string;
                                }
                            }
                            CategoryDataClass? categorydata = null ;
                            var textchannel = socketGuildChannel as SocketTextChannel;
                            if (textchannel != null)
                            {
                                categorydata = server.Categorys.FirstOrDefault(a => a.CategoryID == textchannel.CategoryId);

                            }
                            if (categoryChannel != null)
                            {
                                categorydata = server.Categorys.FirstOrDefault(a => a.CategoryID == categoryChannel.Id);


                            }
                            if (categorydata != null)
                            {
                                if (categorydata.UserId == arg.User.Id || (guser != null && categorydata.RoleId != null && guser.RoleIds.ToList().Contains((ulong)categorydata.RoleId)))
                                {
                                    if (name != null && name != "")
                                    {
                                        try
                                        {
                                            var a = await socketGuildChannel.Guild.CreateTextChannelAsync(name, (a) => { a.CategoryId = categorydata.CategoryID; });
                                            await arg.RespondAsync("<#" + a.Id + ">を作成しました。");
                                            await a.SendMessageAsync(arg.User.Mention + "が管理するチャンネルです");
                                            OverwritePermissions overwrite = new OverwritePermissions(createInstantInvite: PermValue.Inherit, manageChannel: PermValue.Allow, addReactions: PermValue.Inherit, viewChannel: PermValue.Allow, sendMessages: PermValue.Allow, sendTTSMessages: PermValue.Allow, manageMessages: PermValue.Allow, embedLinks: PermValue.Allow, attachFiles: PermValue.Allow, readMessageHistory: PermValue.Allow, mentionEveryone: PermValue.Inherit, useExternalEmojis: PermValue.Allow, connect: PermValue.Inherit, speak: PermValue.Inherit, deafenMembers: PermValue.Inherit, muteMembers: PermValue.Inherit, moveMembers: PermValue.Inherit, useVoiceActivation: PermValue.Inherit, manageRoles: PermValue.Allow, manageWebhooks: PermValue.Inherit, prioritySpeaker: PermValue.Inherit, stream: PermValue.Inherit, useSlashCommands: PermValue.Inherit, useApplicationCommands: PermValue.Inherit, requestToSpeak: PermValue.Inherit, manageThreads: PermValue.Allow, createPrivateThreads: PermValue.Allow, usePrivateThreads: PermValue.Allow, usePublicThreads: PermValue.Allow); ;

                                            await a.AddPermissionOverwriteAsync(arg.User, overwrite);
                                        }
                                        catch (Exception e)
                                        {
                                            await arg.RespondAsync(e.Message);

                                        }


                                    }
                                    else
                                    {
                                        await arg.RespondAsync("名前を指定してください");
                                    }
                                }
                                else
                                {
                                    await arg.RespondAsync("実行権限がありません");
                                }
                            }
                            else
                            {
                                await arg.RespondAsync("作成することを許可されていないカテゴリです");
                            }
                        }
                    }
                }
            }
            else
            {

            }
            await arg.RespondAsync("存在しないコマンドです");
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            
        }

        private async Task _client_Ready()
        {
            Console.WriteLine(_client.CurrentUser + "is Running!!");
            var settingCommand = new SlashCommandBuilder() { Description = "管理者が使用するコマンドです", Name = "admin" }
            
            .AddOption(new SlashCommandOptionBuilder() { Name = "channel-makeable", Description="作成可能なチャンネルの設定をします",Type = ApplicationCommandOptionType.SubCommandGroup}
                .AddOption(new SlashCommandOptionBuilder() {Description = "チャンネルを作成可能なカテゴリを指定します" ,Name = "set", Type = ApplicationCommandOptionType.SubCommand }
                    .AddOption("category",ApplicationCommandOptionType.Channel,"作成可能なカテゴリーを指定します",isRequired:true)
                    .AddOption("user",ApplicationCommandOptionType.Mentionable,"作成可能なロールを指定します",isRequired:true)
                )
                .AddOption(new SlashCommandOptionBuilder() { Description = "チャンネルを作成可能カテゴリを取得します", Name = "get", Type = ApplicationCommandOptionType.SubCommand }
                )
                .AddOption(new SlashCommandOptionBuilder() { Description = "チャンネルを作成可能なカテゴリを削除", Name = "remove", Type = ApplicationCommandOptionType.SubCommand }
                    .AddOption("category", ApplicationCommandOptionType.Channel, "作成可能カテゴリーを作成不可能に指定します", isRequired: true)
                )
                
            ) ;
            var userCommand = new SlashCommandBuilder() { Name = "user", Description = "一般のユーザーが使用するコマンドです" }
            .AddOption(new SlashCommandOptionBuilder() { Name = "channel", Description = "チャンネルに関するコマンドです", Type = ApplicationCommandOptionType.SubCommandGroup }
                .AddOption(new SlashCommandOptionBuilder() { Name = "make", Description = "チャンネルを作成します", Type = ApplicationCommandOptionType.SubCommand }
                    .AddOption("name", ApplicationCommandOptionType.String, "作成したいチャンネル名を指定します", isRequired:true)
                    .AddOption("category", ApplicationCommandOptionType.Channel, "チャンネルを作成するカテゴリを指定します", isRequired: false)

                )
            );
            try
            {
                var t = await _client.GetGlobalApplicationCommandsAsync();
                foreach (var t1 in t)
                {
                    t1.DeleteAsync();
                }
                // With global commands we don't need the guild.
                var commad2 = await _client.CreateGlobalApplicationCommandAsync(userCommand.Build(),new RequestOptions() { AuditLogReason = "チャンネル操作", CancelToken = CancellationToken.None, RetryMode = RetryMode.AlwaysRetry });

                var commad1 = await _client.CreateGlobalApplicationCommandAsync(settingCommand.Build(),new RequestOptions() { AuditLogReason = "チャンネル操作", CancelToken = CancellationToken.None, RetryMode = RetryMode.AlwaysRetry });
                var guild =_client.GetGuild(667884699347058701);
                var comds =await guild.GetApplicationCommandsAsync();
                foreach(var c in comds)
                {
                    _=c.DeleteAsync();
                }


                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (CommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Message, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, Tokens.Token);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

    }
}