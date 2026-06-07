using Newtonsoft.Json;

namespace DiscordServerManager.Data
{
    /// <summary>
    /// サーバーごとの設定データ
    /// </summary>
    public class ServerDataClass
    {
        /// <summary>
        /// サーバー（ギルド）のID
        /// </summary>
        [JsonProperty("ServerID")]
        public ulong ServerID { get; set; }

        /// <summary>
        /// チャンネル作成可能なカテゴリのリスト
        /// </summary>
        [JsonProperty("Categorys")]
        public List<CategoryDataClass> Categorys { get; set; } = new();

        /// <summary>
        /// ボット管理者として登録されているユーザーIDのリスト
        /// </summary>
        [JsonProperty("AdminUserIDs")]
        public List<ulong> AdminUserIDs { get; set; } = new();

        /// <summary>
        /// ボット管理者として登録されているロールIDのリスト
        /// </summary>
        [JsonProperty("AdminRoleIDs")]
        public List<ulong> AdminRoleIDs { get; set; } = new();
    }
}
