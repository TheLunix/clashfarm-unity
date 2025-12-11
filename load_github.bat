@echo off
setlocal ENABLEDELAYEDEXPANSION

REM === Поточна папка, звідки запускається батник ===
set "PROJECT_PATH=%~dp0"
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

echo === Project directory: %PROJECT_PATH%

REM === Переходимо у папку проєкту ===
cd /d "%PROJECT_PATH%" || (
    echo [ERROR] Cannot access project directory.
    pause
    exit /b
)

REM === Перевіряємо, що це git-репозиторій ===
if not exist ".git" (
    echo [ERROR] .git не знайдено у "%PROJECT_PATH%".
    echo Мабуть, папка .git не скопіювалась при перенесенні проєкту.
    echo Якщо це клон з GitHub, заново зроби git clone.
    pause
    exit /b
)

REM === Додаємо теку у safe.directory (для зовнішніх SSD) ===
set "SAFE_DIR=%PROJECT_PATH:\=/%"
git config --global --add safe.directory "%SAFE_DIR%" 1>nul 2>nul

REM === Перевіряємо, що є remote origin ===
git remote get-url origin 1>nul 2>nul || (
    echo [ERROR] Remote 'origin' не налаштований.
    echo Додай його командою:
    echo   git remote add origin https://github.com/<user>/<repo>.git
    pause
    exit /b
)

REM === Визначаємо поточну гілку ===
for /f "usebackq tokens=*" %%B in (`git rev-parse --abbrev-ref HEAD 2^>nul`) do set "CURBR=%%B"

if not defined CURBR (
    echo [INFO] Поточна гілка не визначена. Пробую перейти на main...
    git checkout main 1>nul 2>nul || (
        echo [ERROR] Не можу перейти на main. Створи/обери гілку вручну.
        pause
        exit /b
    )
    set "CURBR=main"
)

echo === Current branch: %CURBR%

REM === Перевіряємо незакомічені зміни ===
set "HAS_CHANGES="
for /f "tokens=1" %%S in ('git status --porcelain') do (
    set "HAS_CHANGES=1"
    goto :HAS_LOCAL_CHANGES
)

goto :DO_PULL

:HAS_LOCAL_CHANGES
echo.
echo [WARN] Є незакомічені локальні зміни у цій гілці.
echo Щоб не словити конфлікти, спочатку:
echo   1) Зроби commit/push (update_github.bat)
echo      АБО
echo   2) Збережи зміни десь і зроби git reset --hard (тільки якщо впевнений).
echo Pull зараз не буде виконано.
pause
exit /b

:DO_PULL
echo.
echo === Pull from origin/%CURBR% (with rebase) ===
git pull --rebase origin "%CURBR%"
if errorlevel 1 (
    echo.
    echo [ERROR] Під час git pull сталася помилка (можливо, конфлікти).
    echo Перевір консоль і виріши конфлікти вручну.
    pause
    exit /b
)

REM === LFS (якщо використовується) ===
git lfs install 1>nul 2>nul
git lfs pull 1>nul 2>nul

echo.
echo ✅ Done: локальна гілка синхронізована з origin/%CURBR%.
pause
endlocal
