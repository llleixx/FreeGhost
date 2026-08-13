param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'icon.png')
)

Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap 256, 256, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::FromArgb(19, 23, 27))

$cyan = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(92, 225, 214))
$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(242, 247, 245))
$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(19, 23, 27))
$fontFamily = New-Object System.Drawing.FontFamily 'Arial Black'
$font = New-Object System.Drawing.Font $fontFamily, 34, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
$textFormat = New-Object System.Drawing.StringFormat
$textFormat.Alignment = [System.Drawing.StringAlignment]::Center
$textFormat.LineAlignment = [System.Drawing.StringAlignment]::Near
$textFormat.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap

$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(80, 70, 96, 96, 180, 180)
$path.AddLine(176, 118, 176, 167)
$path.AddBezier(176, 167, 169, 179, 160, 160, 150, 173)
$path.AddBezier(150, 173, 142, 184, 133, 161, 124, 173)
$path.AddBezier(124, 173, 114, 185, 106, 161, 97, 173)
$path.AddBezier(97, 173, 88, 182, 80, 171, 80, 164)
$path.CloseFigure()
$graphics.FillPath($cyan, $path)

$graphics.FillEllipse($dark, 103, 105, 12, 16)
$graphics.FillEllipse($dark, 140, 105, 12, 16)
$graphics.FillEllipse($white, 124, 130, 8, 8)

$graphics.DrawString('FREE', $font, $white, (New-Object System.Drawing.RectangleF 0, 11, 256, 45), $textFormat)
$graphics.DrawString('GHOST', $font, $white, (New-Object System.Drawing.RectangleF 0, 204, 256, 45), $textFormat)

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}
$bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$path.Dispose()
$textFormat.Dispose()
$font.Dispose()
$fontFamily.Dispose()
$cyan.Dispose()
$white.Dispose()
$dark.Dispose()
$graphics.Dispose()
$bitmap.Dispose()

Write-Output "Generated $OutputPath (256x256 PNG)."
