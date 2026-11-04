# MonitorianNeo 公式サイト（`web/public/monitorianneo/`）

モニターの明るさとコントラストを調整する **MonitorianNeo** の商品ページ一式です。
`web/public/**` への push を契機に `.github/workflows/copy_web.yml` が
`hashimotok-delien/hmk.delien.xyz` の `public/monitorianneo/` へ同期します。

- 公開 URL: `https://hmk.delien.xyz/monitorianneo/`
- 商品名・リンクは `assets/include/config.php` に集約（本文を触る前にここを確認）

---

## 1. 構成

```
web/public/monitorianneo/
├── index.php                  … ランディングページ（自己完結型・インラインCSS）
├── privacy.php                … プライバシーポリシー
├── terms.php                  … 利用規約
├── plans/
│   └── css/
│       └── style.css          … 法務ページ共通のデザインシステム
└── assets/
    ├── include/
    │   ├── config.php         … 商品名・URL・著作権表記などの一元管理
    │   ├── header.php         … 共通ヘッダー
    │   └── footer.php         … 共通フッター
    └── img/                   … アイコン・スクリーンショット（webp / png）
```

| ページ | URL | スタイル |
|---|---|---|
| `index.php` | `/monitorianneo/` | ページ内にインライン `<style>` |
| `privacy.php` | `/monitorianneo/privacy` | `plans/css/style.css` + 最小限の追記 |
| `terms.php` | `/monitorianneo/terms` | `plans/css/style.css` + 最小限の追記 |

---

## 2. 更新の手順

### (1) まず `assets/include/config.php` を確認

商品名・リンク・アクセント色はここだけで全ページに反映されます。
フォルダ名（`monitorianneo`）が URL とそのまま対応するため、安易に変えないこと。

### (2) `assets/include/config.php` の主な変数

ここだけで商品名・リンクが全ページに反映されます。

| 変数 | 内容 |
|---|---|
| `$PRODUCT_NAME` | 表示名（`MonitorianNeo`） |
| `$PRODUCT_EMOJI` | ロゴ横の絵文字（不要なら `''`） |
| `$PRODUCT_SLUG` | フォルダ名。ランディングのページ遷移アニメの判定に使用 |
| `$LOGO_LABEL` | ヘッダーロゴのラベル（`Mixer` など。各ページで上書き可） |
| `$DOCS_URL` | ドキュメントサイトの URL（別ドメインなら絶対URL） |
| `$SUPPORT_URL` | サポート窓口（GitHub Issues など） |
| `$ICON_PATH` | アイコン画像への相対パス |
| `$ACCENT_RGB` | アクセント色の `R,G,B`（JS の遷移アニメ用） |
| `$SITE_OWNER` | `<title>` の `\|` 以降（例: `けい🤖`） |
| `$COPYRIGHT_HOLDER` / `$COPYRIGHT_URL` | フッターの著作権表記 |

> **注意:** `config.php` は必ず「何も出力しない」状態に保つこと。
> 各ページが `<!DOCTYPE html>` より前に `require` しているため、閉じタグ後の空白や改行でも
> 出力が始まるとレイアウトが崩れます（末尾に `?>` を書かないのが安全）。

### (3) 本文を埋める

- `index.php` … キャッチコピー、できること、導入メリット、導入手順、使用イメージ
- `privacy.php` / `terms.php` … 文面を変えたら制定日・最終改定日も合わせて更新する

### (4) 画像を自前配信する

`assets/img/` に置き、`index.php` からは `./assets/img/...` で参照します。

```
assets/img/icon.webp          … ヒーローのアイコン（120px 表示 / 400px 程度を用意）
assets/img/screenshot-1.png   … 使用イメージ（メイン画面）
assets/img/screenshot-2.png   … 使用イメージ（一括調整）
```

Gyazo などの外部URLを直接貼るのは避けてください（リンク切れ・外部依存・表示遅延の原因）。

### (5) ポートフォリオ／ドキュメントからリンクする

- ポートフォリオ（`hmk.delien.xyz` 側の `public/index.php`）の該当カードに `href="./monitorianneo/"` を追加
- ドキュメントサイトを用意する場合は doc-hub 側に `docs/monitorianneo/` を作成し、`docs/index.php` のポータルにも追加
- 内部リンクは**拡張子なし**で書く（`.php` は付けない。フォルダのトップは `./` か `monitorianneo/`）

### (6) フォントのサブセット対象に追加

フォントのサブセット生成（`tools/build_fonts.py`）は doc-hub 側のリポジトリにあります。
ページ内の文字を大きく変えたときは、doc-hub の `PUBLIC_PAGES` に
`monitorianneo/index.php` が入っていることを確認し、`python tools/build_fonts.py` を実行して
`public/assets/fonts/` を再生成してください。

### (7) エージェント定義に構成メモを追記

`.github/agents/navi.agent.md` にフォルダ構成のメモを追記しておくと、
以後の作業で構成の把握がぶれません。

---

## 3. 気をつけること（重要）

### URL・パス

- **拡張子なしURLが正規形。** `public/.htaccess` が `/foo.php` → `/foo` に301し、
  拡張子なしアクセスは内部で `foo.php` を実行します。`/foo/index.php` は `/foo/` に集約。
  - 内部リンクは `terms`、`plans/`、`./` のように書く。`terms.php` は書かない。
- **`$BASE` はページごとに違う。**
  - `privacy.php` / `terms.php`（商品ルート）… `$BASE = '.'`
  - `plans/*.php`（1階層下）… `$BASE = '..'`
  - `header.php` / `footer.php` は `<?= $BASE ?? '.' ?>` を使うので、**include 前に必ず代入する**
- **このサイトはドキュメントルート直下の1階層（`public/monitorianneo/`）前提。**
  `index.php` の favicon が `../favicon.png`、フォントが `../assets/fonts/` なのはそのため。
  さらに深い階層に置く場合はパスを全部見直すこと。
- 法務ページの CSS は `plans/css/style.css` を**相対パス**で読んでいる（`plans/css/style.css`）。
  商品ルートからの相対で書く。

### include の扱い

- `assets/include/` はサーバーサイド専用。`public/.htaccess` の
  `RewriteRule (^|/)include(/|$) - [F,L]` で直接アクセスが 403 になります。
  設定ファイルを置く場所としても安全ですが、**必ず `include/` の中に置く**こと。
- include は PHP の `include` で行う（JS で読み込まない）。
- `header.php` / `footer.php` は `require_once __DIR__ . '/config.php';` で設定を自己完結して
  読み込むので、ページ側で二重に require しても問題ありません。

### CSS

- **アクセント色は `#00e5ff`。** テーマカラーを勝手に変えない。
- ハードコードせず、カスタムプロパティを使う。
  - `index.php` のインラインCSS … `:root { --accent; --accent-rgb }`
  - `plans/css/style.css` … `:root { --bg-primary; --accent; --border; --radius; ... }`
- 半透明は `rgba(var(--accent-rgb), .15)` の形で書く。
  **`--accent` を変えたら `--accent-rgb`（と `config.php` の `$ACCENT_RGB`）も必ず揃える。**
- `plans/css/style.css` は `privacy.php` / `terms.php` と共有。編集すると3ページに影響する。
  法務ページ固有の見た目は各ページの `<style>` に閉じ込める。

### キャッシュ破棄

- CSS は `?v=<?= @filemtime(__DIR__ . '/css/style.css') ?: time() ?>` の形で読み込む。
  更新日時がクエリに入るので、ファイルを差し替えるだけでブラウザキャッシュが切れる。
- 画像はこの仕組みの対象外。**差し替えたらファイル名を変える**（または `?v=` を手で付ける）。

### SEO・メタ情報

- 各ページに `<title>` と `<meta name="description">` を必ず書く。
- ランディングには OGP / Twitter カード（`og:image` は 1200x630 の絶対URL）を設定する。
- 課金ページ（`plans/success.php` など）を将来追加する場合は `noindex` を付ける
  （決済フローの結果ページを検索結果に出さない）。
- 新しいページを公開したら、サイトマップや `robots.txt` を運用している場合は追加する。

### 法務

- `privacy.php` / `terms.php` は**雛形**。公開前に必ず自サービス用に書き直す。
- 本サービスは無料配布のため、特定商取引法に基づく表記は不要。
  将来有料化する場合は、事業者名・連絡先・支払時期などの表記と、
  決済事業者（Stripe など）の名前・データの取扱いのプライバシーポリシーへの明記が必要。

### アクセシビリティ・表示

- `index.php` の `section` は IntersectionObserver でフェードインする。
  `prefers-reduced-motion: reduce` の環境ではアニメーションを止めているので、消さないこと。
- 画像には必ず `alt` を書く（スクリーンショットは内容が分かる説明にする）。
- モバイル（〜600px）で文字サイズ・余白が崩れないか確認する。

### 壊しやすい箇所

- `index.php` の**ページ遷移アニメ**（`#page-transition`）は、`a` 要素のクリックを全部
  横取りして 500ms 後に遷移させます。`target="_blank"` と `#` 始まりのリンクは除外済み。
  リンクを増やすときはこの条件に注意（外部リンクには `target="_blank" rel="noopener"`）。
- `$PRODUCT_SLUG` は遷移アニメの色分け判定に使うので、**フォルダ名と一致させる**。

---

## 4. 動作確認

```powershell
# 構文チェック
Get-ChildItem web\public\monitorianneo -Recurse -Filter *.php | ForEach-Object { php -l $_.FullName }

# ローカルプレビュー（web/public をドキュメントルートにする）
php -S localhost:8000 -t web/public
# → http://localhost:8000/monitorianneo/ を開く
```

- [ ] PHP の Warning / Fatal が出ていない（`@filemtime` は失敗時に `time()` にフォールバック）
- [ ] アイコン・スクリーンショット・favicon が 404 になっていない
- [ ] ヘッダーのロゴ／ナビ、フッターの利用規約リンクが正しい階層を指している
- [ ] モバイル幅でレイアウトが崩れていない
- [ ] 法務ページ（`/monitorianneo/privacy`・`/monitorianneo/terms`）が 200 で開く

---

## 5. 参考

- 同期先: `hashimotok-delien/hmk.delien.xyz`（`public/monitorianneo/`）
- デザインの基準: doc-hub の `docs/` 配下にある共通カラースキーム
- フォント生成: doc-hub の `tools/build_fonts.py`
