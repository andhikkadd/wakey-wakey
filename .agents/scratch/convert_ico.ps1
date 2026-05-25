Add-Type -AssemblyName System.Drawing
$pngPath = "c:\learm\wakeywakey\src\WakeyWakey\Resources\logo.png"
$icoPath = "c:\learm\wakeywakey\src\WakeyWakey\Resources\app.ico"

if (Test-Path $pngPath) {
    $bmp = [System.Drawing.Bitmap]::FromFile($pngPath)
    $hIcon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $stream = [System.IO.File]::Create($icoPath)
    $icon.Save($stream)
    $stream.Close()
    $bmp.Dispose()
    Write-Output "Successfully created $icoPath"
} else {
    Write-Error "Source PNG not found at $pngPath"
}
