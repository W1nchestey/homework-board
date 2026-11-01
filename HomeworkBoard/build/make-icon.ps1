Add-Type -AssemblyName System.Drawing

$outDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$icoPath = Join-Path $outDir "app.ico"
$previewPath = Join-Path $outDir "icon-preview.png"
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$payloads = @()

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $pad = [Math]::Max(1.0, $size * 0.055)
    $radius = [Math]::Max(2.0, $size * 0.24)
    $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($size - 2 * $pad), ($size - 2 * $pad))
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.Left, $rect.Top, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Top, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.Left, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(233, 109, 66),
        [System.Drawing.Color]::FromArgb(201, 73, 38),
        45.0)
    $graphics.FillPath($brush, $path)

    $penWidth = [Math]::Max(1.4, $size * 0.075)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $penWidth)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $points = @(
        (New-Object System.Drawing.PointF(($size * 0.29), ($size * 0.52))),
        (New-Object System.Drawing.PointF(($size * 0.44), ($size * 0.67))),
        (New-Object System.Drawing.PointF(($size * 0.72), ($size * 0.36)))
    )
    $graphics.DrawLines($pen, $points)

    $graphics.Dispose()
    $pen.Dispose()
    $brush.Dispose()
    $path.Dispose()

    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $payloads += ,$stream.ToArray()
    if ($size -eq 256) {
        $bitmap.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $stream.Dispose()
    $bitmap.Dispose()
}

$file = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $payload = $payloads[$i]
    $dimension = if ($size -ge 256) { 0 } else { $size }
    $writer.Write([Byte]$dimension)
    $writer.Write([Byte]$dimension)
    $writer.Write([Byte]0)
    $writer.Write([Byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$payload.Length)
    $writer.Write([UInt32]$offset)
    $offset += $payload.Length
}
for ($i = 0; $i -lt $payloads.Count; $i++) {
    $writer.Write($payloads[$i])
}
$writer.Close()
$file.Close()
Write-Output "Icon written: $icoPath"
