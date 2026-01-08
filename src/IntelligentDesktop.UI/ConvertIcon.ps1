
Add-Type -AssemblyName System.Drawing

$sourcePath = "app_icon.png"
$destPath = "app.ico"
$currentPath = Get-Location

$fullSource = Join-Path $currentPath $sourcePath
$fullDest = Join-Path $currentPath $destPath

Write-Host "Source: $fullSource"

if (-not (Test-Path $fullSource)) {
    Write-Error "Source file not found: $fullSource"
    exit 1
}

$bmp = [System.Drawing.Bitmap]::FromFile($fullSource)
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = $null

try {
    $fs = New-Object System.IO.FileStream($fullDest, "Create")
    $icon.Save($fs)
    Write-Host "Converted to $fullDest"
}
finally {
    if ($fs -ne $null) { $fs.Dispose() }
    if ($icon -ne $null) { $icon.Dispose() }
    if ($bmp -ne $null) { $bmp.Dispose() }
}
