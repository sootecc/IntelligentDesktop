@echo off
chcp 65001 > nul
echo 🚀 Intelligent Desktop 설치를 시작합니다...

:: 관리자 권한 확인 (필요시)
:: 여기서는 AppData에 설치하므로 관리자 권한 불필요

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\IntelligentDesktop"
set "SOURCE_EXE=IntelligentDesktop.UI.exe"

if not exist "%~dp0Publish\%SOURCE_EXE%" (
    echo ❌ 에러: 설치 파일(Publish\%SOURCE_EXE%)을 찾을 수 없습니다.
    echo 먼저 publish.ps1을 실행하여 빌드해주세요.
    pause
    exit /b
)

echo 📂 설치 폴더 생성: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo 📦 파일 복사 중...
copy /Y "%~dp0Publish\%SOURCE_EXE%" "%INSTALL_DIR%\"
copy /Y "%~dp0Publish\*.dll" "%INSTALL_DIR%\" 2>nul
copy /Y "%~dp0Publish\*.json" "%INSTALL_DIR%\" 2>nul

echo 🔗 바로가기 생성 중...
powershell -Command "$s=(New-Object -COM 'WScript.Shell');$s.CreateShortcut('%USERPROFILE%\Desktop\Intelligent Desktop.lnk').TargetPath='%INSTALL_DIR%\%SOURCE_EXE%';$s.CreateShortcut('%USERPROFILE%\Desktop\Intelligent Desktop.lnk').Save()"

echo ✅ 설치가 완료되었습니다!
echo 바탕화면의 'Intelligent Desktop' 아이콘을 실행하세요.
pause
