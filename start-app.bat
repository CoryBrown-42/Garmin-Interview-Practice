@echo off
REM Adds Node.js to PATH for this session (if not already)
set "NODE_PATH=C:\Program Files\nodejs"
set "PATH=%NODE_PATH%;%PATH%"

REM Start the Vite dev server
npm run dev

pause