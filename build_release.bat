@echo off
setlocal

set "ROOT_DIR=%~dp0"
set "MSBUILD_EXE=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if not exist "%MSBUILD_EXE%" (
  echo MSBuild not found: %MSBUILD_EXE%
  exit /b 1
)

"%MSBUILD_EXE%" "%ROOT_DIR%ImageMove.sln" /t:Build /p:Configuration=Release /m
if errorlevel 1 exit /b %errorlevel%

echo release\ImageMove.exe updated.
exit /b 0
