# OGP 画像 (1200x630) をアイコンとテキストから生成する
param(
    [Parameter(Mandatory)][string]$IconPath,
    [Parameter(Mandatory)][string]$Destination,
    [string]$Title = 'MonitorianNeo',
    [string]$Tagline = 'モニターの明るさを、まとめて手早く。',
    [string]$SubLine = '個別／一括の明るさ・コントラスト調整  /  Windows 向け 無料ツール'
)

Add-Type -AssemblyName System.Drawing

$W = 1200; $H = 630
$canvas = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# 背景
$g.Clear([System.Drawing.Color]::FromArgb(255, 10, 10, 15))

# 左下のアクセント円
$accentCircle = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 10, 32, 40))
$g.FillEllipse($accentCircle, -220, 200, 700, 700)

# アイコン（角丸の影付き）
$iconSize = 200
$iconX = 110; $iconY = [int](($H - $iconSize) / 2)
$shadowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$r = 40; $sx = $iconX - 6; $sy = $iconY - 6; $sw = $iconSize + 12; $sh = $iconSize + 12
$shadowPath.AddArc($sx, $sy, $r, $r, 180, 90)
$shadowPath.AddArc($sx + $sw - $r, $sy, $r, $r, 270, 90)
$shadowPath.AddArc($sx + $sw - $r, $sy + $sh - $r, $r, $r, 0, 90)
$shadowPath.AddArc($sx, $sy + $sh - $r, $r, $r, 90, 90)
$shadowPath.CloseFigure()
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(140, 0, 229, 255))
$g.FillPath($shadowBrush, $shadowPath)

$icon = [System.Drawing.Image]::FromFile((Resolve-Path $IconPath))
$g.DrawImage($icon, (New-Object System.Drawing.Rectangle($iconX, $iconY, $iconSize, $iconSize)))
$icon.Dispose()

# アクセントの縦線
$barBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 229, 255))
$g.FillRectangle($barBrush, 370, 218, 6, 78)

# テキスト
$titleFont = New-Object System.Drawing.Font('Segoe UI', 54, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$taglineFont = New-Object System.Drawing.Font('Yu Gothic UI', 26, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$subFont = New-Object System.Drawing.Font('Yu Gothic UI', 19, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)

$titleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 224, 224, 224))
$taglineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 229, 255))
$subBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 170, 170, 180))

$g.DrawString($Title, $titleFont, $titleBrush, 400, 196)
$g.DrawString($Tagline, $taglineFont, $taglineBrush, 400, 292)
$g.DrawString($SubLine, $subFont, $subBrush, 402, 342)

# 枠線（うっすら）
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(30, 0, 229, 255), 2)
$g.DrawRectangle($borderPen, 1, 1, $W - 2, $H - 2)

$g.Dispose()
$canvas.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()

"written: $Destination ($((Get-Item $Destination).Length) bytes)"
