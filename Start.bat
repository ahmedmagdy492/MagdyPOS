@echo off
echo **** MagdyPOS ****
echo ------------------

start "" MagdyPOS.exe
timeout /t 4 /nobreak >nul
start http://localhost:5000/