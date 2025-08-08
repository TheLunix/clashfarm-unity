@echo off
REM === Налаштування ===
set PROJECT_PATH=C:\Farm Clash\FarmClash
set COMMIT_MSG=Auto update %date% %time%

REM === Перехід у папку проєкту ===
cd /d "%PROJECT_PATH%"

REM === Додавання всіх змін ===
git add .

REM === Коміт з повідомленням ===
git commit -m "%COMMIT_MSG%"

REM === Відправка в GitHub ===
git push origin main

pause