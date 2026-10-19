@echo off
setlocal
cd /d "%~dp0"

where py >nul 2>nul
if %errorlevel%==0 (
  py -3 app.py
  goto :done
)

where python >nul 2>nul
if %errorlevel%==0 (
  python app.py
  goto :done
)

echo.
echo Python 3 was not found.
echo Install Python 3.9+ from https://www.python.org/downloads/
echo and tick "Add Python to PATH" during installation.
echo.
pause

:done
endlocal
