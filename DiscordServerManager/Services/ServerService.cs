using DiscordServerManager.Data;

namespace DiscordServerManager.Services
{
    public class ServerService
    {
        private readonly string _serversDirectory;

        public ServerService(string serversDirectory)
        {
            _serversDirectory = serversDirectory;
        }

        public ServerDataClass GetServerData(ulong guildId)
        {
            var path = Path.Combine(_serversDirectory, guildId + ".xml");

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path, System.Text.Encoding.UTF8);
                return XMLClass.LoadFromFile<ServerDataClass>(text);
            }

            return new ServerDataClass { ServerID = guildId };
        }

        public void SaveServerData(ServerDataClass server)
        {
            var path = Path.Combine(_serversDirectory, server.ServerID + ".xml");
            var save = XMLClass.SaveToFile(server);
            File.WriteAllText(path, save, System.Text.Encoding.UTF8);
        }
    }
}
