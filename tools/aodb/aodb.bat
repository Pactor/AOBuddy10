@echo off
setlocal
title AODB - Anarchy Online reference

rem ---------------------------------------------------------------------------
rem Starts the AODB reference web app and opens it in the browser.
rem
rem   aodb.bat          serve on 8888
rem   aodb.bat 9000     serve on 9000, for when something else has 8888
rem
rem It reads the generated profile data under AOBuddy\GameData\profiles and needs
rem nothing but Python 3 - no packages to install.
rem ---------------------------------------------------------------------------

set "PORT=%~1"
if "%PORT%"=="" set "PORT=8888"

where python >nul 2>nul
if errorlevel 1 (
  echo ERROR: python was not found on PATH.
  echo Install Python 3 and tick "Add python.exe to PATH", then run this again.
  pause
  exit /b 1
)

echo Starting AODB on port %PORT% ...
start "" "http://localhost:%PORT%/"
python "%~dp0app.py" %PORT%

rem Python exits non-zero when the port is taken or the argument was nonsense;
rem hold the window open so the reason is readable instead of flashing past.
if errorlevel 1 pause
endlocal
