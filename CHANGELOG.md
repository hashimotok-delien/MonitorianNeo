# Changelog

MonitorianNeo の変更履歴です。書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に沿っています。

MonitorianNeo は、複数モニターの明るさとコントラストを調整する
[Monitorian](https://github.com/emoacht/Monitorian)（作者: emoacht）のフォークです。
Monitorian 4.16.9 を基点にしています。

## [0.1.2] - 2026-11-05

### 追加

- **更新確認**を追加。トレイメニューの「更新を確認」で GitHub Releases の最新版を調べ、新しいバージョンがあればそのままダウンロードしてインストールできます（SHA-256 を検証してから実行）
- **起動時の更新確認**と**自動更新**をトレイメニューから設定できるように。既定では起動時に確認のみ行い、自動更新はオフです
- ポータブル版（インストーラーを使わずに展開した場合）は自動更新せず、リリースページを開きます

### 変更

- アプリ本体・トレイ・インストーラー・公式サイト（OGP 画像を含む）のアイコンを新しいロゴに統一

## [0.1.1] - 2026-11-05

### 修正

- 自己完結・単一ファイル版で、起動直後に `ArgumentNullException` が発生する問題を修正。単一ファイルでは `Assembly.Location` が空になるためスタートアップ登録のパスが取得できず、例外が記録されていた（実行ファイルのパスは `Environment.ProcessPath` から取得するように変更）
- 通知領域のアイコンが読み込めず `IOException` が発生する問題を修正。.NET 8 への移行でトレイアイコンが埋め込みリソースから漏れていた

## [0.1.0] - 2026-11-05

### 追加

- MonitorianNeo として最初のリリース（Monitorian 4.16.9 をフォーク）
- GitHub Releases での配布（インストーラーとポータブル ZIP）

### 変更

- 実行ファイル名を `MonitorianNeo.exe`、ライブラリ名を `MonitorianNeo.Core.dll` に変更
- .NET Framework 4.8 から **.NET 8** へ移行（自己完結・単一ファイルで配布。ランタイムの別途インストールは不要）
- Windows 7 / 8.1 のサポートを終了（動作要件は Windows 10 1607 以降 / Windows 11 x64）
- インストーラーを WiX（MSI）から Inno Setup（setup.exe）に変更
- インストーラーの製品名を `MonitorianNeo` に変更
- 設定の保存先を `%LOCALAPPDATA%\MonitorianNeo` に変更
- Microsoft Store 版向けの記述（Store 配布・winget・サブスクリプションのアドオン）を削除し、
  アドオン相当の機能はすべて無償で利用できるように
- 公式サイトの内容を本アプリ（モニターの明るさ・コントラスト調整）に合わせて刷新
