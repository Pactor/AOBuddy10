@echo off
REM Build navbridge as 32-bit, because the AO client DLLs it calls are 32-bit.
setlocal
set VC=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars32.bat
if not exist "%VC%" set VC=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars32.bat
if not exist "%VC%" set VC=C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars32.bat
if not exist "%VC%" (
  echo Could not find a 32-bit MSVC environment ^(vcvars32.bat^).
  exit /b 1
)
call "%VC%" >nul
cd /d "%~dp0"
cl /nologo /EHa /O2 /W3 /D_CRT_SECURE_NO_WARNINGS navbridge.cpp /Fe:navbridge.exe /link /SUBSYSTEM:CONSOLE
endlocal
