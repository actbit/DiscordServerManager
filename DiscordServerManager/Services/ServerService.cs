using Discord;
using DiscordServerManager.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordServerManager.Services
{
    public class ServerService
    {
        private readonly string _serversDirectory;

        public ServerService(string serversDirectory)
        {
            _serversDirectory = serversDirectory;
            Console.WriteLine($"[ServerService] Initialized with directory: {_serversDirectory}");

            try
            {
                MigrateXmlToJson();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] XML migration failed: {ex.Message}");
            }

            try
            {
                MigrateCategoryDataFormat();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Category data format migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// サーバーデータを取得します
        /// </summary>
        public ServerDataClass GetServerData(ulong guildId)
        {
            var jsonPath = GetServerDataPath(guildId);
            Console.WriteLine($"[ServerService] GetServerData for Guild {guildId}, path: {jsonPath}");

            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    var data = JsonConvert.DeserializeObject<ServerDataClass>(json);
                    if (data != null)
                    {
                        Console.WriteLine($"[ServerService] Loaded data for Guild {guildId}: Categories={data.Categorys.Count}, AdminUsers={data.AdminUserIDs.Count}, AdminRoles={data.AdminRoleIDs.Count}");
                        return data;
                    }
                    Console.WriteLine($"[WARN] Deserialize returned null for Guild {guildId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] データ読み込みエラー ({guildId}): {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[ServerService] Data file not found for Guild {guildId}: {jsonPath}");
            }

            return new ServerDataClass { ServerID = guildId };
        }

        /// <summary>
        /// サーバーデータを保存します
        /// </summary>
        public void SaveServerData(ServerDataClass server)
        {
            try
            {
                var jsonPath = GetServerDataPath(server.ServerID);
                var json = JsonConvert.SerializeObject(server, Formatting.Indented);
                File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
                Console.WriteLine($"[ServerService] Saved data for Guild {server.ServerID}: {jsonPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] データ保存エラー ({server.ServerID}): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 存在しないカテゴリ/ロール/ユーザーのIDを削除してクリーンアップ
        /// </summary>
        public async Task CleanupServerDataAsync(IGuild guild)
        {
            var server = GetServerData(guild.Id);
            bool modified = false;

            // 存在しないカテゴリを削除
            modified |= await CleanupCategoriesAsync(server, guild);

            // 各カテゴリで存在しないロールを削除
            modified |= CleanupCategoryRoles(server, guild);

            // 管理者ロールから存在しないロールを削除
            modified |= CleanupAdminRoles(server, guild);

            // 管理者ユーザーから存在しないユーザーを削除（オフラインユーザーもいるため慎重に）
            modified |= await CleanupAdminUsersAsync(server, guild);

            if (modified)
            {
                SaveServerData(server);
                Console.WriteLine($"[Cleanup] クリーンアップ完了: {guild.Name}");
            }
        }

        // プライベートヘルパーメソッド
        private string GetServerDataPath(ulong guildId)
            => Path.Combine(_serversDirectory, $"{guildId}.json");

        private async Task<bool> CleanupCategoriesAsync(ServerDataClass server, IGuild guild)
        {
            var categoriesToRemove = new List<CategoryDataClass>();
            foreach (var c in server.Categorys)
            {
                var channel = await guild.GetChannelAsync(c.CategoryID);
                if (channel == null)
                    categoriesToRemove.Add(c);
            }

            foreach (var category in categoriesToRemove)
            {
                server.Categorys.Remove(category);
                Console.WriteLine($"[Cleanup] 削除されたカテゴリを除外: {category.CategoryID}");
            }

            return categoriesToRemove.Any();
        }

        private bool CleanupCategoryRoles(ServerDataClass server, IGuild guild)
        {
            var guildRoleIds = guild.Roles.Select(r => r.Id).ToHashSet();
            bool modified = false;

            foreach (var category in server.Categorys)
            {
                var rolesToRemove = category.RoleIds
                    .Where(roleId => !guildRoleIds.Contains(roleId))
                    .ToList();

                foreach (var roleId in rolesToRemove)
                {
                    category.RoleIds.Remove(roleId);
                    Console.WriteLine($"[Cleanup] カテゴリから削除されたロールを除外: {roleId}");
                    modified = true;
                }
            }

            return modified;
        }

        private bool CleanupAdminRoles(ServerDataClass server, IGuild guild)
        {
            var guildRoleIds = guild.Roles.Select(r => r.Id).ToHashSet();
            var rolesToRemove = server.AdminRoleIDs
                .Where(roleId => !guildRoleIds.Contains(roleId))
                .ToList();

            foreach (var roleId in rolesToRemove)
            {
                server.AdminRoleIDs.Remove(roleId);
                Console.WriteLine($"[Cleanup] 削除された管理者ロールを除外: {roleId}");
            }

            return rolesToRemove.Any();
        }

        private async Task<bool> CleanupAdminUsersAsync(ServerDataClass server, IGuild guild)
        {
            // オフラインユーザーもいるため、取得できたユーザーのみチェック
            var users = await guild.GetUsersAsync();
            var guildUserIds = users.Select(u => u.Id).ToHashSet();
            var usersToRemove = server.AdminUserIDs
                .Where(userId => !guildUserIds.Contains(userId))
                .ToList();

            foreach (var userId in usersToRemove)
            {
                server.AdminUserIDs.Remove(userId);
                Console.WriteLine($"[Cleanup] 削除された管理者ユーザーを除外: {userId}");
            }

            return usersToRemove.Any();
        }

        /// <summary>
        /// 古い形式（RoleId, UserIdが単一値）から新しい形式（RoleIds, UserIdsがリスト）に移行
        /// </summary>
        private void MigrateCategoryDataFormat()
        {
            var jsonFiles = Directory.GetFiles(_serversDirectory, "*.json");

            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    var jObj = JObject.Parse(json);
                    var categorys = jObj["Categorys"] as JArray;

                    if (categorys == null) continue;

                    bool modified = false;

                    foreach (var category in categorys)
                    {
                        modified |= MigrateRoleId(category);
                        modified |= MigrateUserId(category);
                    }

                    if (modified)
                    {
                        File.WriteAllText(jsonPath, jObj.ToString(Formatting.Indented), System.Text.Encoding.UTF8);
                        Console.WriteLine($"[Migration] カテゴリデータ形式移行完了: {Path.GetFileNameWithoutExtension(jsonPath)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] カテゴリデータ形式移行エラー ({Path.GetFileName(jsonPath)}): {ex.Message}");
                }
            }
        }

        private bool MigrateRoleId(JToken category)
        {
            var oldRoleId = category["RoleId"];
            if (oldRoleId == null || category["RoleIds"] != null)
                return false;

            // 無効な値（null・空文字列・0）の場合はマイグレーションしない
            if (oldRoleId.Type == JTokenType.Null)
                return false;

            var raw = oldRoleId.ToObject<string>();
            if (string.IsNullOrWhiteSpace(raw) || !ulong.TryParse(raw, out var value) || value == 0)
                return false;

            var roleIds = new JArray { value };
            category["RoleIds"] = roleIds;
            category["RoleId"]?.Remove();
            return true;
        }

        private bool MigrateUserId(JToken category)
        {
            var oldUserId = category["UserId"];
            if (oldUserId == null || category["UserIds"] != null)
                return false;

            // 無効な値（null・空文字列・0）の場合はマイグレーションしない
            if (oldUserId.Type == JTokenType.Null)
                return false;

            var raw = oldUserId.ToObject<string>();
            if (string.IsNullOrWhiteSpace(raw) || !ulong.TryParse(raw, out var value) || value == 0)
                return false;

            var userIds = new JArray { value };
            category["UserIds"] = userIds;
            category["UserId"]?.Remove();
            return true;
        }

        /// <summary>
        /// 古いXML形式のデータをJSONに移行
        /// </summary>
        private void MigrateXmlToJson()
        {
            var xmlFiles = Directory.GetFiles(_serversDirectory, "*.xml");

            foreach (var xmlPath in xmlFiles)
            {
                try
                {
                    var guildId = Path.GetFileNameWithoutExtension(xmlPath);
                    var jsonPath = Path.Combine(_serversDirectory, guildId + ".json");

                    ServerDataClass data = LoadXmlData(xmlPath, jsonPath);

                    // XML内の古い形式（RoleId/UserId）を新しい形式（RoleIds/UserIds）に移行
                    foreach (var category in data.Categorys)
                    {
                        if (category.RoleId.HasValue && category.RoleId != 0 && !category.RoleIds.Contains(category.RoleId.Value))
                        {
                            category.RoleIds.Add(category.RoleId.Value);
                        }
                        if (category.UserId.HasValue && category.UserId != 0 && !category.UserIds.Contains(category.UserId.Value))
                        {
                            category.UserIds.Add(category.UserId.Value);
                        }
                    }

                    // ServerIDが0の場合はファイル名から設定
                    if (data.ServerID == 0 && ulong.TryParse(guildId, out var serverId))
                    {
                        data.ServerID = serverId;
                        Console.WriteLine($"[Migration] ServerID修正: {serverId}");
                    }

                    SaveServerData(data);
                    File.Delete(xmlPath);

                    Console.WriteLine($"[Migration] XML→JSON移行完了: {guildId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] XML移行エラー ({Path.GetFileName(xmlPath)}): {ex.Message}");
                }
            }
        }

        private ServerDataClass LoadXmlData(string xmlPath, string jsonPath)
        {
            var xmlText = File.ReadAllText(xmlPath, System.Text.Encoding.UTF8);
            var xmlData = XMLClass.LoadFromFile<ServerDataClass>(xmlText);

            if (!File.Exists(jsonPath))
                return xmlData;

            // 両方ある場合はXMLデータをJSONデータにマージして返す
            var jsonText = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
            var jsonData = JsonConvert.DeserializeObject<ServerDataClass>(jsonText);

            if (jsonData != null)
            {
                MergeData(xmlData, jsonData);
                return jsonData;
            }

            return xmlData;
        }

        /// <summary>
        /// source（XML側）のデータを target（JSON側）に統合する
        /// target を更新して返す想定
        /// </summary>
        private void MergeData(ServerDataClass source, ServerDataClass target)
        {
            // カテゴリデータのマージ（XML側を優先して上書き、JSON側にしかないものは保持）
            foreach (var category in source.Categorys)
            {
                var existing = target.Categorys.FirstOrDefault(c => c.CategoryID == category.CategoryID);
                if (existing != null)
                {
                    target.Categorys.Remove(existing);
                }
                target.Categorys.Add(category);
            }

            // 管理者ユーザーのマージ（重複しないものを追加）
            foreach (var adminUser in source.AdminUserIDs)
            {
                if (!target.AdminUserIDs.Contains(adminUser))
                {
                    target.AdminUserIDs.Add(adminUser);
                }
            }

            // 管理者ロールのマージ（重複しないものを追加）
            foreach (var adminRole in source.AdminRoleIDs)
            {
                if (!target.AdminRoleIDs.Contains(adminRole))
                {
                    target.AdminRoleIDs.Add(adminRole);
                }
            }
        }
    }
}
