# icon.png から Windows 用 .ico を生成する
# - 小さいサイズ(<=64) は 32bpp DIB + AND マスク（System.Drawing.Icon 互換）
# - 大きいサイズ(>=128) は PNG 圧縮（Vista 以降のシェル用）
param(
    [Parameter(Mandatory)][string]$Source,
    [Parameter(Mandatory)][string]$Destination,
    [string[]]$Sizes = @('16', '20', '24', '32', '40', '48', '64', '128', '256')
)

Add-Type -AssemblyName System.Drawing

$sourceBmp = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source))
if ($sourceBmp.Width -ne $sourceBmp.Height) {
    throw "元画像が正方形ではありません: $($sourceBmp.Width)x$($sourceBmp.Height)"
}

function New-ResizedBitmap {
    param([System.Drawing.Bitmap]$Bitmap, [int]$Size)
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
        $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($Bitmap, (New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)))
    }
    finally { $g.Dispose() }
    return $bmp
}

function Get-DibBytes {
    param([System.Drawing.Bitmap]$Bitmap)
    $w = $Bitmap.Width; $h = $Bitmap.Height

    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $data = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $stride = $data.Stride
        $raw = New-Object byte[] ($stride * $h)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $raw, 0, $raw.Length)
    }
    finally { $Bitmap.UnlockBits($data) }

    $maskStride = [int](([math]::Floor(($w + 31) / 32)) * 4)
    $maskSize = $maskStride * $h
    $pixelSize = $w * $h * 4

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # BITMAPINFOHEADER
    $bw.Write([uint32]40)              # biSize
    $bw.Write([int32]$w)               # biWidth
    $bw.Write([int32]($h * 2))         # biHeight (XOR + AND)
    $bw.Write([uint16]1)               # biPlanes
    $bw.Write([uint16]32)              # biBitCount
    $bw.Write([uint32]0)               # biCompression = BI_RGB
    $bw.Write([uint32]$pixelSize)      # biSizeImage
    $bw.Write([int32]0)                # biXPelsPerMeter
    $bw.Write([int32]0)                # biYPelsPerMeter
    $bw.Write([uint32]0)               # biClrUsed
    $bw.Write([uint32]0)               # biClrImportant

    # ピクセル（BGRA・下から上）
    for ($y = $h - 1; $y -ge 0; $y--) {
        $bw.Write($raw, ($y * $stride), ($w * 4))
    }

    # AND マスク（アルファで表現するので全 0）
    $bw.Write((New-Object byte[] $maskSize), 0, $maskSize)

    $bw.Flush()
    return $ms.ToArray()
}

function Get-PngBytes {
    param([System.Drawing.Bitmap]$Bitmap)
    $ms = New-Object System.IO.MemoryStream
    $Bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return $ms.ToArray()
}

# 各サイズの画像データを作成
$entries = @()
foreach ($sizeText in $Sizes) {
    $size = [int]$sizeText
    $bmp = New-ResizedBitmap -Bitmap $sourceBmp -Size $size
    try {
        $bytes = if ($size -ge 128) { Get-PngBytes -Bitmap $bmp } else { Get-DibBytes -Bitmap $bmp }
    }
    finally { $bmp.Dispose() }
    $entries += [pscustomobject]@{ Size = $size; Bytes = $bytes }
}

# ICO を書き出す
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([uint16]0)                      # reserved
$bw.Write([uint16]1)                      # type = icon
$bw.Write([uint16]$entries.Count)         # count

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $bw.Write([byte]$dim)                 # width
    $bw.Write([byte]$dim)                 # height
    $bw.Write([byte]0)                    # color count
    $bw.Write([byte]0)                    # reserved
    $bw.Write([uint16]1)                  # planes
    $bw.Write([uint16]32)                 # bit count
    $bw.Write([uint32]$e.Bytes.Length)    # bytes in resource
    $bw.Write([uint32]$offset)            # offset
    $offset += $e.Bytes.Length
}
foreach ($e in $entries) { $bw.Write($e.Bytes, 0, $e.Bytes.Length) }
$bw.Flush()

[IO.File]::WriteAllBytes($Destination, $out.ToArray())
$sourceBmp.Dispose()

"written: $Destination ($([IO.File]::ReadAllBytes($Destination).Length) bytes, sizes=$($Sizes -join ','))"
