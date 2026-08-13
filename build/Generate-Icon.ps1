param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'icon.png')
)

Add-Type -AssemblyName System.Drawing

$bitmap = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::FromArgb(19, 23, 27))

$cyan = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(92, 225, 214))
$white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(242, 247, 245))
$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(19, 23, 27))
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(242, 247, 245)), 8
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(48, 35, 160, 160, 180, 180)
$path.AddLine(208, 115, 208, 196)
$path.AddBezier(208, 196, 196, 216, 181, 184, 165, 205)
$path.AddBezier(165, 205, 151, 224, 137, 185, 121, 205)
$path.AddBezier(121, 205, 104, 225, 91, 185, 76, 205)
$path.AddBezier(76, 205, 61, 220, 48, 202, 48, 190)
$path.CloseFigure()
$graphics.FillPath($cyan, $path)

$graphics.FillEllipse($dark, 87, 93, 20, 27)
$graphics.FillEllipse($dark, 149, 93, 20, 27)

$graphics.DrawLine($pen, 128, 15, 128, 45)
$graphics.DrawLine($pen, 113, 30, 128, 15)
$graphics.DrawLine($pen, 143, 30, 128, 15)
$graphics.FillEllipse($white, 121, 135, 14, 14)

$directory = Split-Path -Parent $OutputPath
if ($directory -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}
$bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)

$path.Dispose()
$pen.Dispose()
$cyan.Dispose()
$white.Dispose()
$dark.Dispose()
$graphics.Dispose()
$bitmap.Dispose()

Write-Output "Generated $OutputPath (256x256 PNG)."
