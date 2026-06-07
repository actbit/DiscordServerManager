using Newtonsoft.Json;
using System.Xml.Serialization;

namespace DiscordServerManager.Data
{
    /// <summary>
    /// カテゴリごとのチャンネル作成権限設定
    /// </summary>
    public class CategoryDataClass
    {
        /// <summary>
        /// カテゴリのID
        /// </summary>
        [JsonProperty("CategoryID")]
        public ulong CategoryID { get; set; }

        /// <summary>
        /// 古い形式: 単一ロールID（XMLからのマイグレーション用）
        /// </summary>
        [JsonIgnore]
        public ulong? RoleId { get; set; }

        /// <summary>
        /// 古い形式: 単一ユーザーID（XMLからのマイグレーション用）
        /// </summary>
        [JsonIgnore]
        public ulong? UserId { get; set; }

        /// <summary>
        /// このカテゴリでチャンネル作成権限を持つロールIDのリスト
        /// </summary>
        [JsonProperty("RoleIds")]
        public List<ulong> RoleIds { get; set; } = new();

        /// <summary>
        /// このカテゴリでチャンネル作成権限を持つユーザーIDのリスト
        /// </summary>
        [JsonProperty("UserIds")]
        public List<ulong> UserIds { get; set; } = new();
    }
}
