# DiscordServerManager

Discordサーバーでユーザーが自由にチャンネルを作成できるようにするボットです。

## 機能

- ユーザーが指定したカテゴリ内でチャンネルを作成可能
- カテゴリごとにチャンネル作成権限を持つユーザー・ロールを設定
- ボット管理者の管理（サーバーオーナー専用）
- 自動データクリーンアップ（削除されたカテゴリ/ロール/ユーザーのIDを除去）

## セットアップ

### 1. 必要条件

- .NET 10.0 SDK
- Discord Bot Token

### 2. 設定

1. `config.json` を `bin/Debug/net10.0/` フォルダに作成:

```json
{
  "Token": "あなたのボットトークン"
}
```

2. Discord Developer Portalで以下の権限を有効にする:
   - Bot Permissions:
     - チャンネルを管理
     - ロールを管理
     - メッセージを送信
     - メッセージ履歴を読む
     - チャンネルを見る

3. Gateway Intents:
   - SERVER MEMBERS INTENT を有効化

### 3. 実行

```bash
dotnet run
```

## コマンド一覧

### ユーザーコマンド

| コマンド | 説明 |
|---------|------|
| `/user channel make <チャンネル名> [カテゴリ]` | 指定したカテゴリ（省略時は現在のチャンネルのカテゴリ）にチャンネルを作成 |

### 管理者コマンド

#### チャンネル作成設定 (`/admin channel-makeable`)

| コマンド | 説明 |
|---------|------|
| `/admin channel-makeable add-user <カテゴリ> <ユーザー>` | カテゴリにチャンネル作成権限を持つユーザーを追加 |
| `/admin channel-makeable add-role <カテゴリ> <ロール>` | カテゴリにチャンネル作成権限を持つロールを追加 |
| `/admin channel-makeable remove-user <カテゴリ> <ユーザー>` | カテゴリからユーザーを削除 |
| `/admin channel-makeable remove-role <カテゴリ> <ロール>` | カテゴリからロールを削除 |
| `/admin channel-makeable list` | チャンネル作成可能なカテゴリ一覧を表示 |
| `/admin channel-makeable remove <カテゴリ>` | カテゴリを完全に削除 |

#### ボット管理者設定 (`/admin manager`) - サーバーオーナー専用

| コマンド | 説明 |
|---------|------|
| `/admin manager add-user <ユーザー>` | ボット管理者ユーザーを追加 |
| `/admin manager add-role <ロール>` | ボット管理者ロールを追加 |
| `/admin manager remove-user <ユーザー>` | ボット管理者ユーザーを削除 |
| `/admin manager remove-role <ロール>` | ボット管理者ロールを削除 |
| `/admin manager list` | ボット管理者一覧を表示 |

## 権限の優先順位

1. **サーバーオーナー** - すべてのコマンドが使用可能
2. **ボット管理者** - `/admin` コマンド全体が使用可能
   - AdminUserIDs に含まれるユーザー
   - AdminRoleIDs に含まれるロールを持つユーザー
3. **一般ユーザー** - `/user` コマンドのみ使用可能
   - カテゴリの UserIds/RoleIds に含まれる場合のみチャンネル作成可能

## データ保存

サーバーごとの設定は `Servers/サーバーID.json` に保存されます。

```json
{
  "ServerID": 123456789,
  "Categorys": [
    {
      "CategoryID": 987654321,
      "RoleIds": [111111111],
      "UserIds": [222222222]
    }
  ],
  "AdminUserIDs": [333333333],
  "AdminRoleIDs": [444444444]
}
```

## 自動マイグレーション

- 起動時に古いXML形式のデータを自動でJSONに変換
- 古いデータ形式（単一値）を新しい形式（リスト）に自動変換
- 削除されたカテゴリ/ロール/ユーザーのIDを自動クリーンアップ
