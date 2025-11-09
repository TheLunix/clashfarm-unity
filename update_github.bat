@echo off
REM === Налаштування ===
set "PROJECT_PATH=D:\Farm Clash\FarmClash"
set "COMMIT_MSG=Auto update %date% %time%"

REM === Перехід у папку проєкту ===
cd /d "%PROJECT_PATH%"

REM === Додаємо теку у безпечний список Git ===
git config --global --add safe.directory "D:/Farm Clash/FarmClash"

REM === Додавання всіх змін ===
git add .

REM === Коміт з повідомленням ===
git commit -m "%COMMIT_MSG%"

REM === Відправка в GitHub ===
git push origin main

pause
