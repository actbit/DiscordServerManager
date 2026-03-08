using Newtonsoft.Json;

namespace DiscordServerManager.Data
{
    public class CategoryDataClass
    {
        [JsonProperty("CategoryID")]
        public ulong CategoryID { get; set; }

        [JsonProperty("RoleId")]
        public ulong? RoleId { get; set; }

        [JsonProperty("UserId")]
        public ulong? UserId { get; set; }
    }
}
