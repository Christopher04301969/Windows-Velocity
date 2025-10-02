#!/bin/bash
# restore_snapshot.sh
# Restore latest snapshot
LATEST_OS=$(ls -d /mnt/c/Snapshots/OS_* | sort -r | head -n 1)
if [ -z "$LATEST_OS" ]; then
    echo "No OS snapshot found" > /mnt/c/Windows/Temp/restore_log.txt
    exit 1
fi
reg import $LATEST_OS/hklm.reg
reg import $LATEST_OS/hkcu.reg
cp -r $LATEST_OS/config /mnt/c/Windows/System32/config
echo "OS snapshot restored from $LATEST_OS" > /mnt/c/Windows/Temp/restore_log.txt