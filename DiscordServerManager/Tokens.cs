using Newtonsoft.Json;
using System;
using System.IO;

namespace DiscordServerManager
{
    internal class Tokens
    {
        public static string Token { get; private set; } = "";

        public static void LoadFromFile(string basePath)
        {
            string configPath = Path.Combine(basePath, "config.json");

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"設定ファイルが見つかりません: {configPath}");
            }

            string json = File.ReadAllText(configPath, System.Text.Encoding.UTF8);
            var config = JsonConvert.DeserializeObject<ConfigData>(json);

            if (config == null || string.IsNullOrEmpty(config.Token))
            {
                throw new Exception("config.jsonにTokenが設定されていません");
            }

            Token = config.Token;
        }
    }

    internal class ConfigData
    {
        public string Token { get; set; } = "";
    }
}
