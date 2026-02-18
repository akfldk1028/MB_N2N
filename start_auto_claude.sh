#!/bin/bash
# ============================================
# Auto-Claude 24/7 Daemon for MB_N2N BrickGame
# ============================================
#
# Usage: ./start_auto_claude.sh
# Background: nohup ./start_auto_claude.sh > daemon_output.log 2>&1 &
#

echo "[Auto-Claude] Starting 24/7 Daemon for MB_N2N BrickGame..."
echo "[Auto-Claude] Project: D:/Data/MB_N2N/MB_N2N"
echo "[Auto-Claude] Specs: .auto-claude/specs/ (8 tasks queued)"
echo ""

cd "D:/Data/AC247_MB/AC247/Auto-Claude/apps/backend"

python3 runners/daemon_runner.py \
  --project-dir "D:/Data/MB_N2N/MB_N2N" \
  --max-concurrent 2 \
  --use-claude-cli \
  --status-file "D:/Data/MB_N2N/MB_N2N/.auto-claude/daemon_status.json" \
  --log-file "D:/Data/MB_N2N/MB_N2N/.auto-claude/daemon.log" \
  --stuck-timeout 900

echo ""
echo "[Auto-Claude] Daemon stopped."
