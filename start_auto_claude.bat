@echo off
REM ============================================
REM Auto-Claude 24/7 Daemon for MB_N2N BrickGame
REM ============================================
REM
REM 사전 준비:
REM   1. Python 3.10+ 설치
REM   2. cd D:\Data\AC247_MB\AC247\Auto-Claude\apps\backend
REM   3. pip install -r requirements.txt
REM   4. .env 파일에 CLAUDE_CODE_OAUTH_TOKEN 설정
REM      또는: claude setup-token 실행
REM
REM 실행: 이 파일을 더블클릭하거나 cmd에서 실행
REM ============================================

echo [Auto-Claude] Starting 24/7 Daemon for MB_N2N BrickGame...
echo [Auto-Claude] Project: D:\Data\MB_N2N\MB_N2N
echo [Auto-Claude] Specs: .auto-claude\specs\ (8 tasks queued)
echo.

cd /d D:\Data\AC247_MB\AC247\Auto-Claude\apps\backend

REM 가상환경 활성화
call .venv\Scripts\activate.bat

python runners\daemon_runner.py ^
  --project-dir "D:\Data\MB_N2N\MB_N2N" ^
  --max-concurrent 2 ^
  --use-claude-cli ^
  --status-file "D:\Data\MB_N2N\MB_N2N\.auto-claude\daemon_status.json" ^
  --log-file "D:\Data\MB_N2N\MB_N2N\.auto-claude\daemon.log" ^
  --stuck-timeout 900

echo.
echo [Auto-Claude] Daemon stopped.
pause
