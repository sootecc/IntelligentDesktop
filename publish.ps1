# Intelligent Desktop Publish Script
$ErrorActionPreference = "Stop"

Write-Host "🚧 Building IntelligentDesktop..." -ForegroundColor Yellow

$projectPath = "src\IntelligentDesktop.UI\IntelligentDesktop.UI.csproj"
$outputDir = "Publish"

# Clean previous build
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}

# Publish as Single File
dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o $outputDir

# Copy missing dll if any (Sometimes single file excludes some native deps)
# But .NET 6+ usually handles it well.

Write-Host "✅ Build Complete! Access files in '$outputDir'" -ForegroundColor Green
