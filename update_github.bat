@echo off
setlocal ENABLEDELAYEDEXPANSION

REM === Поточна папка, звідки запускається батник ===
set "PROJECT_PATH=%~dp0"
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

REM === Повідомлення коміту ===
set "COMMIT_MSG=Auto update %date% %time%"

echo === Project directory: %PROJECT_PATH%

REM === Переходимо у папку проєкту ===
cd /d "%PROJECT_PATH%" || (
    echo [ERROR] Cannot access project directory.
    pause
    exit /b
)

REM === Додаємо теку у safe.directory (для зовнішніх SSD) ===
set "SAFE_DIR=%PROJECT_PATH:\=/%"
git config --global --add safe.directory "%SAFE_DIR%" 1>nul 2>nul

REM === Додавання всіх змін ===
git add .

REM === Коміт (лише якщо є зміни) ===
for /f "tokens=1" %%S in ('git status --porcelain') do (
    set "HAS_CHANGES=1"
    goto :DO_COMMIT
)

echo [INFO] No changes to commit. Pushing anyway...
git push origin main
goto :END

:DO_COMMIT
echo Committing: %COMMIT_MSG%
git commit -m "%COMMIT_MSG%"

REM === Відправка в GitHub ===
git push origin main

:END
echo Done.
pause
endlocal
