using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscordServerManager.Data
{
    public class ServerDataClass
    {
        [JsonProperty("ServerID")]
        public ulong ServerID { get; set; }

        [JsonProperty("Categorys")]
        public List<CategoryDataClass> Categorys { get; set; } = new();

        [JsonProperty("AdminUserIDs")]
        public List<ulong> AdminUserIDs { get; set; } = new();

        [JsonProperty("AdminRoleIDs")]
        public List<ulong> AdminRoleIDs { get; set; } = new();
    }
}
