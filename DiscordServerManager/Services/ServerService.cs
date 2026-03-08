using DiscordServerManager.Data;
using Newtonsoft.Json;

namespace DiscordServerManager.Services
{
    public class ServerService
    {
        private readonly string _serversDirectory;

        public ServerService(string serversDirectory)
        {
            _serversDirectory = serversDirectory;
            MigrateXmlToJson();
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
