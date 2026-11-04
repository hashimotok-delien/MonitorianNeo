# Changelog

MonitorianNeo の変更履歴です。書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に沿っています。

MonitorianNeo は、複数モニターの明るさとコントラストを調整する
[Monitorian](https://github.com/emoacht/Monitorian)（作者: emoacht）のフォークです。
Monitorian 4.16.9 を基点にしています。

## [0.1.0] - 2026-11-05

### 追加

- MonitorianNeo として最初のリリース（Monitorian 4.16.9 をフォーク）
- GitHub Releases での配布（インストーラーとポータブル ZIP）

### 変更

- 実行ファイル名を `MonitorianNeo.exe`、ライブラリ名を `MonitorianNeo.Core.dll` に変更
- インストーラーの製品名を `MonitorianNeo` に変更
- 設定の保存先を `%LOCALAPPDATA%\MonitorianNeo` に変更
- Microsoft Store 版向けの記述（Store 配布・winget・サブスクリプションのアドオン）を削除し、
  アドオン相当の機能はすべて無償で利用できるように
- 公式サイトの内容を本アプリ（モニターの明るさ・コントラスト調整）に合わせて刷新
