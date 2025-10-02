#!/bin/bash
# verify_packages.sh
# Verify installer packages in WSL
for pkg in "$@"
do
    echo "Verifying $pkg"
    sha256sum "/mnt/c/Windows/Temp/$pkg" > "/mnt/c/Windows/Temp/$pkg.sha256"
done