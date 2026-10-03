# Roguelite

# 命名規則 (Naming Conventions)

本プロジェクトにおけるC#コードの命名規則は、原則として[Microsoftの公式ガイドライン](https://learn.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/identifier-names)に準拠します。

## 一覧表

| 対象 | 適用ルール | 具体例 | 備考 |
|---|---|---|---|
| クラス・構造体 | PascalCase | `PlayerController`, `GameManager` | |
| インターフェイス | `I` + PascalCase | `IWorkerQueue`, `IEquippable` | 先頭に `I` を付与 |
| メソッド (関数) | PascalCase | `StartGame()`, `CalculateDamage()` | |
| プロパティ | PascalCase | `HitPoints`, `IsActive` | |
| イベント (event) | PascalCase | `EventProcessing`, `PlayerDied` | 状態変化を表す動詞(過去形/進行形等)を推奨 |
| メンバ変数 (private) | `_` + camelCase | `_playerSpeed`, `_weaponData` | |
| メンバ変数 (static) | `s_` + camelCase | `s_maxPlayers`, `s_instance` | |
| ローカル変数 | camelCase | `currentIndex`, `resultValue` | |
| 定数 | PascalCase | `MaxHealth`, `DefaultSpeed` | |
| 列挙型 (Enum) | PascalCase | `GameState`, `WeaponType` | |
