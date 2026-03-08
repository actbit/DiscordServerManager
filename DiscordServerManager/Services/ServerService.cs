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
            MigrateXmlToJson();
            MigrateCategoryDataFormat();
        }

        /// <summary>
        /// 存在しないカテゴリ/ロールのIDを削除してクリーンアップ
        /// </summary>
        public void CleanupServerData(IGuild guild)
        {
            var server = GetServerData(guild.Id);
            bool modified = false;

            // 存在しないカテゴリを削除
            var categoriesToRemove = server.Categorys
                .Where(c => guild.GetChannelAsync(c.CategoryID).Result == null)
                .ToList();

            foreach (var category in categoriesToRemove)
            {
                server.Categorys.Remove(category);
                Console.WriteLine($"[Cleanup] 削除されたカテゴリを除外: {category.CategoryID}");
                modified = true;
            }

            // 各カテゴリで存在しないロールを削除
            var guildRoleIds = guild.Roles.Select(r => r.Id).ToHashSet();

            foreach (var category in server.Categorys)
            {
                var rolesToRemove = category.RoleIds
                    .Where(roleId => !guildRoleIds.Contains(roleId))
                    .ToList();

                foreach (var roleId in rolesToRemove)
                {
                    category.RoleIds.Remove(roleId);
                    Console.WriteLine($"[Cleanup] 削除されたロールを除外: {roleId}");
                    modified = true;
                }
            }

            // AdminRoleIDsから存在しないロールを削除
            var adminRolesToRemove = server.AdminRoleIDs
                .Where(roleId => !guildRoleIds.Contains(roleId))
                .ToList();

            foreach (var roleId in adminRolesToRemove)
            {
                server.AdminRoleIDs.Remove(roleId);
                Console.WriteLine($"[Cleanup] 削除された管理者ロールを除外: {roleId}");
                modified = true;
            }

            // AdminUserIDsから存在しないユーザーを削除
            // 注意: オフラインユーザーもいるため、取得できたユーザーのみチェック
            var guildUserIds = guild.GetUsersAsync().Result.Select(u => u.Id).ToHashSet();

            var adminUsersToRemove = server.AdminUserIDs
                .Where(userId => !guildUserIds.Contains(userId))
                .ToList();

            foreach (var userId in adminUsersToRemove)
            {
                server.AdminUserIDs.Remove(userId);
                Console.WriteLine($"[Cleanup] 削除された管理者ユーザーを除外: {userId}");
                modified = true;
            }

            if (modified)
            {
                SaveServerData(server);
            }
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
                        // 古い RoleId → 新しい RoleIds
                        var oldRoleId = category["RoleId"];
                        if (oldRoleId != null && category["RoleIds"] == null)
                        {
                            var roleIds = new JArray();
                            if (oldRoleId.Type != JTokenType.Null && oldRoleId.Value<ulong>() != 0)
                            {
                                roleIds.Add(oldRoleId.Value<ulong>());
                            }
                            category["RoleIds"] = roleIds;
                            category["RoleId"]?.Remove();
                            modified = true;
                        }

                        // 古い UserId → 新しい UserIds
                        var oldUserId = category["UserId"];
                        if (oldUserId != null && category["UserIds"] == null)
                        {
                            var userIds = new JArray();
                            if (oldUserId.Type != JTokenType.Null && oldUserId.Value<ulong>() != 0)
                            {
                                userIds.Add(oldUserId.Value<ulong>());
                            }
                            category["UserIds"] = userIds;
                            category["UserId"]?.Remove();
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        File.WriteAllText(jsonPath, jObj.ToString(Formatting.Indented), System.Text.Encoding.UTF8);
                        Console.WriteLine($"カテゴリデータ形式移行完了: {Path.GetFileNameWithoutExtension(jsonPath)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"カテゴリデータ形式移行エラー ({Path.GetFileName(jsonPath)}): {ex.Message}");
                }
            }
        }

        private void MigrateXmlToJson()
        {
            var xmlFiles = Directory.GetFiles(_serversDirectory, "*.xml");

            foreach (var xmlPath in xmlFiles)
            {
                var guildId = Path.GetFileNameWithoutExtension(xmlPath);
                var jsonPath = Path.Combine(_serversDirectory, guildId + ".json");

                ServerDataClass data;

                if (File.Exists(jsonPath))
                {
                    // 両方ある場合はマージ
                    var xmlText = File.ReadAllText(xmlPath, System.Text.Encoding.UTF8);
                    var jsonData = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);

                    var xmlData = XMLClass.LoadFromFile<ServerDataClass>(xmlText);
                    var jsonDataObj = JsonConvert.DeserializeObject<ServerDataClass>(jsonData);

                    if (jsonDataObj != null)
                    {
                        // XMLのデータを優先してマージ
                        MergeData(xmlData, jsonDataObj);
                        data = xmlData;
                    }
                    else
                    {
                        data = xmlData;
                    }

                    Console.WriteLine($"マージ完了: {guildId}");
                }
                else
                {
                    // XMLのみ → JSONに変換
                    var xmlText = File.ReadAllText(xmlPath, System.Text.Encoding.UTF8);
                    data = XMLClass.LoadFromFile<ServerDataClass>(xmlText);
                    Console.WriteLine($"移行完了: {guildId}");
                }

                // ServerIDが0の場合はファイル名から設定
                if (data.ServerID == 0 && ulong.TryParse(guildId, out var serverId))
                {
                    data.ServerID = serverId;
                    Console.WriteLine($"ServerID修正: {serverId}");
                }

                // JSON保存
                SaveServerData(data);

                // XML削除
                File.Delete(xmlPath);
            }
        }

        private void MergeData(ServerDataClass source, ServerDataClass target)
        {
            // sourceのデータを優先
            foreach (var category in source.Categorys)
            {
                var existing = target.Categorys.FirstOrDefault(c => c.CategoryID == category.CategoryID);
                if (existing != null)
                {
                    target.Categorys.Remove(existing);
                }
                target.Categorys.Add(category);
            }

            foreach (var adminUser in source.AdminUserIDs)
            {
                if (!target.AdminUserIDs.Contains(adminUser))
                {
                    target.AdminUserIDs.Add(adminUser);
                }
            }

            foreach (var adminRole in source.AdminRoleIDs)
            {
                if (!target.AdminRoleIDs.Contains(adminRole))
                {
                    target.AdminRoleIDs.Add(adminRole);
                }
            }
        }

        public ServerDataClass GetServerData(ulong guildId)
        {
            var jsonPath = Path.Combine(_serversDirectory, guildId + ".json");

            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<ServerDataClass>(json);
                if (data != null) return data;
            }

            return new ServerDataClass { ServerID = guildId };
        }

        public void SaveServerData(ServerDataClass server)
        {
            var jsonPath = Path.Combine(_serversDirectory, server.ServerID + ".json");
            var json = JsonConvert.SerializeObject(server, Formatting.Indented);
            File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
        }
    }
}
