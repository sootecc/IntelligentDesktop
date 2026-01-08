@echo off
chcp 65001 > nul
echo 🗑️ Intelligent Desktop 제거를 시작합니다...

set "INSTALL_DIR=%LOCALAPPDATA%\Programs\IntelligentDesktop"
set "APP_NAME=IntelligentDesktop"

echo 🛑 프로세스 종료 중...
taskkill /F /IM "IntelligentDesktop.UI.exe" 2>nul

echo 🧹 레지스트리 정리 중 (자동 실행)...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "%APP_NAME%" /f 2>nul

echo 📂 파일 삭제 중...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

echo 🔗 바로가기 삭제 중...
del "%USERPROFILE%\Desktop\Intelligent Desktop.lnk" 2>nul

echo ✅ 제거가 완료되었습니다.
echo 이용해 주셔서 감사합니다.
pause
