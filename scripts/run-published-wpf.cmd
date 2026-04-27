@echo off
setlocal

set ROOT=%~dp0..
set API_DIR=%ROOT%\publish\webapi-win-x64
set WPF_DIR=%ROOT%\publish\wpfclient-win-x64
set API_EXE=%API_DIR%\SmartContentRecommender.WebAPI.exe
set WPF_EXE=%WPF_DIR%\SmartContentRecommender.WpfClient.exe

if not exist "%API_EXE%" (
  echo [ERROR] API exe not found: %API_EXE%
  exit /b 1
)

if not exist "%WPF_EXE%" (
  echo [ERROR] WPF exe not found: %WPF_EXE%
  exit /b 1
)

echo Starting WebAPI...
start "SCR WebAPI" /D "%API_DIR%" "%API_EXE%"

timeout /t 3 /nobreak > nul

echo Starting WPF client...
start "SCR WPF Client" /D "%WPF_DIR%" "%WPF_EXE%"

echo WPF stack started.
exit /b 0
