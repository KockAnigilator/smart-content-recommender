@echo off
setlocal

set ROOT=%~dp0..
set API_DIR=%ROOT%\publish\webapi-win-x64
set WEB_DIR=%ROOT%\publish\webclient-win-x64
set API_EXE=%API_DIR%\SmartContentRecommender.WebAPI.exe
set WEB_EXE=%WEB_DIR%\SmartContentRecommender.WebClient.exe

if not exist "%API_EXE%" (
  echo [ERROR] API exe not found: %API_EXE%
  exit /b 1
)

if not exist "%WEB_EXE%" (
  echo [ERROR] WebClient exe not found: %WEB_EXE%
  exit /b 1
)

echo Starting WebAPI...
start "SCR WebAPI" /D "%API_DIR%" "%API_EXE%"

timeout /t 3 /nobreak > nul

echo Starting WebClient...
start "SCR WebClient" /D "%WEB_DIR%" "%WEB_EXE%"

timeout /t 2 /nobreak > nul
start "" "http://localhost:5133"

echo Web stack started. If browser did not open, go to http://localhost:5133
exit /b 0
