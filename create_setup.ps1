# Intelligent Desktop Installer Generator (Final Fix)
$ErrorActionPreference = "Stop"

Write-Host "🔨 Building Project..." -ForegroundColor Yellow
& .\publish.ps1

$baseDir = Get-Location
$publishDir = Join-Path $baseDir "Publish"
$installBat = Join-Path $baseDir "install.bat"
$outputExeName = "IntelligentDesktop_Setup.exe"
$finalOutput = Join-Path $baseDir $outputExeName

# 임시 폴더 사용 (경로 공백/한글 문제 원천 차단)
$tempWorkDir = Join-Path $env:TEMP "ID_Setup_$(Get-Random)"
New-Item -ItemType Directory -Force -Path $tempWorkDir | Out-Null

try {
    Write-Host "📂 Copying files to temp: $tempWorkDir"
    Copy-Item "$publishDir\*" $tempWorkDir -Recurse
    Copy-Item $installBat $tempWorkDir
    
    $sedFile = Join-Path $tempWorkDir "installer.sed"
    $targetExe = Join-Path $tempWorkDir $outputExeName
    
    # 파일명 리스트 생성
    $files = Get-ChildItem $tempWorkDir | Where-Object { $_.Name -ne "installer.sed" -and $_.Name -ne $outputExeName }
    $fileListString = ""
    foreach ($f in $files) {
        $fileListString += "$($f.Name)=`r`n"
    }

    # 중요: [Strings] 섹션의 값에는 따옴표를 넣으면 안 됩니다. (IExpress 파싱 오류 원인)
    $sedContent = @"
[Version]
Class=IEXPRESS
SEDVersion=3.0
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=
DisplayLicense=
FinishMessage=Installation Completed!
TargetName=$targetExe
FriendlyName=Intelligent Desktop Setup
AppLaunched=cmd.exe /c install.bat
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
[SourceFiles]
SourceFiles0=$tempWorkDir\
[SourceFiles0]
$fileListString
"@

    $sedContent | Out-File $sedFile -Encoding Default

    Write-Host "💿 Running IExpress..." -ForegroundColor Cyan
    iexpress /N $sedFile

    if (Test-Path $targetExe) {
        Move-Item $targetExe $finalOutput -Force
        Write-Host "✅ Setup created: $finalOutput" -ForegroundColor Green
    } else {
        Write-Error "IExpress failed to create output file."
    }
}
finally {
    if (Test-Path $tempWorkDir) {
        Remove-Item $tempWorkDir -Recurse -Force
    }
}
