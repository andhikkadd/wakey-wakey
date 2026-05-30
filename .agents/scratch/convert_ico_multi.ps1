Add-Type -AssemblyName System.Drawing
$sourcePath = "c:\learm\wakeywakey\src\WakeyWakey\Resources\logo.png"
$outputPath = "c:\learm\wakeywakey\src\WakeyWakey\Resources\app.ico"

if (-not (Test-Path $sourcePath)) {
    Write-Error "Source PNG not found at $sourcePath"
    exit 1
}

# Load source image
$srcBmp = [System.Drawing.Bitmap]::FromFile($sourcePath)

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngBytesList = @()

foreach ($sz in $sizes) {
    $destBmp = New-Object System.Drawing.Bitmap($sz, $sz)
    $g = [System.Drawing.Graphics]::FromImage($destBmp)
    
    # Set high quality resize settings
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($srcBmp, 0, 0, $sz, $sz)
    
    $ms = New-Object System.IO.MemoryStream
    $destBmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytesList += ,$ms.ToArray()
    
    $ms.Dispose()
    $g.Dispose()
    $destBmp.Dispose()
}
$srcBmp.Dispose()

# Create ICO file
$fs = [System.IO.File]::Create($outputPath)
$w = New-Object System.IO.BinaryWriter($fs)

# Header
$w.Write([UInt16]0) # Reserved
$w.Write([UInt16]1) # Type (1 = Icon)
$w.Write([UInt16]$sizes.Count) # Number of images

# Calculate offsets
# The directory is 6 bytes (header) + 16 bytes * count
$currentOffset = 6 + 16 * $sizes.Count

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $sz = $sizes[$i]
    $bytes = $pngBytesList[$i]
    
    # Width and Height (0 means 256)
    $wVal = if ($sz -eq 256) { 0 } else { $sz }
    $w.Write([Byte]$wVal)
    $w.Write([Byte]$wVal)
    $w.Write([Byte]0) # Color count (0 for >256 colors)
    $w.Write([Byte]0) # Reserved
    $w.Write([UInt16]1) # Color planes
    $w.Write([UInt16]32) # Bits per pixel
    $w.Write([UInt32]$bytes.Length) # Image size in bytes
    $w.Write([UInt32]$currentOffset) # Offset from start of file
    
    $currentOffset += $bytes.Length
}

# Write PNG datas
foreach ($bytes in $pngBytesList) {
    $w.Write($bytes)
}

$w.Close()
$fs.Close()
Write-Output "ICO successfully created with all resolutions!"
