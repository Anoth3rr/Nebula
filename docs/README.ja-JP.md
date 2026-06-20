[English](../README.md) | [简体中文](./README.zh-CN.md) | 日本語


# Nebula

Nebula は、複数のパブリッシャーのゲームを統合するために開発されたオープンソースのサードパーティ製ランチャーです。HoYoverse のすべての PC ゲーム、Hypergryph のすべての PC ゲーム、Wuthering Waves に対応し、中国国内の一部の二次元ゲームを統合することを目標にしています。ランチャーの基本機能に加えて、個人的な需要に応じて以下のような拡張機能も追加しています:

-  ゲームのプレイ時間を記録
-  ゲームアカウントの切り替え
-  ゲームのスクリーンショットを表示
-  ガチャ記録を保存
-  ゲームサーバーの切り替え

さらに多くの機能は、ぜひ実際に探索してみてください。


## Fork について

このプロジェクトは [Scighost/Starward](https://github.com/Scighost/Starward) から fork されています。また、[wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker) と [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha) のアイデアや実装も参考にしています。


## 更新履歴

### v0.0.0

- Arknights、Arknights: Endfield、Wuthering Waves のサポート項目を追加し、利用可能な範囲で中国本土版とグローバル版のゲーム識別子を追加しました。
- Wuthering Waves のガチャ記録に対応しました。アイテムメタデータと取得ロジックは [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker) を参考にしています。
- Arknights: Endfield のガチャ記録に対応しました。アカウント連携、token ベースの取得フロー、キャラクター/武器プール解析、インポートと統計連携を含み、[bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha) を参考にしています。
- Skland Wiki から Endfield のアイテムメタデータを同期し、ローカルの `EndfieldGachaInfo` に保存して、アイコン照合と既存ガチャ記録のアイテム名更新を行う機能を追加しました。
- URL Protocol ドキュメントを拡張し、`endfield_cn`、`endfield_global`、`wutheringwaves_cn`、`wutheringwaves_global` のゲーム識別子を追加しました。
- Genshin Impact、Honkai: Star Rail、Genshin Impact、Arknights の公式サーバーと Bilibili サーバーの切り替えに対応しました。


## ダウンロード

始めに使用しているデバイスが以下の条件を満たしている必要があります:

- Windows 10 1809 (17763) 以降
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) がインストール済みであること
- [WebP Image Extension](https://apps.microsoft.com/detail/9pg2dk419drg) がインストール済みであること
- より良い体験のため、システム設定で **透明効果** と **アニメーション効果** を有効にしてください

>[WebP Image Extension](https://apps.microsoft.com/detail/9pg2dk419drg) は通常システムに同梱されています。アプリで背景画像が正しく表示されない場合は、インストールされているか確認してください。

[GitHub Release](https://github.com/Anoth3rr/Nebula/releases) から CPU アーキテクチャに対応したパッケージをダウンロードして展開し、`Nebula.exe` を実行して表示される案内に従ってください。


## 開発

プロジェクトをローカルでコンパイルするには、Visual Studio 2022 をインストールし、以下のワークロードを選択する必要があります:

-  .NET デスクトップ開発
-  C++ によるデスクトップ開発
-  ユニバーサル Windows プラットフォーム開発


## 謝辞

このプロジェクトは以下のプロジェクトを参考にし、多くの恩恵を受けています。各プロジェクトの作者とコミュニティに心より感謝します:

-  [Scighost/Starward](https://github.com/Scighost/Starward): このプロジェクトの fork 元です。
-  [CollapseLauncher/Collapse](https://github.com/CollapseLauncher/Collapse): ランチャーの設計と実装の考え方を参考にしました。
-  [DGP-Studio/Snap.Hutao](https://github.com/DGP-Studio/Snap.Hutao): 開発中に参考にし、関連する助力も受けました。
-  [wuwatracker/wuwatracker](https://github.com/wuwatracker/wuwatracker): Wuthering Waves のガチャ記録とアイテムメタデータ関連で参考にしました。
-  [bhaoo/endfield-gacha](https://github.com/bhaoo/endfield-gacha): Arknights: Endfield のガチャ記録関連で参考にしました。

および、本プロジェクトで使用している[サードパーティライブラリ](./ThirdParty.md)。
