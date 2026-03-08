using Newtonsoft.Json;

namespace DiscordServerManager.Data
{
    public class CategoryDataClass
    {
        [JsonProperty("CategoryID")]
        public ulong CategoryID { get; set; }

        [JsonProperty("RoleIds")]
        public List<ulong> RoleIds { get; set; } = new();

        [JsonProperty("UserIds")]
        public List<ulong> UserIds { get; set; } = new();
    }
}
