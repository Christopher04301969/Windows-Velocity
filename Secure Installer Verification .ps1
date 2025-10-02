$verifyScript = @"
#!/bin/bash
for pkg in ""\$@""
do
    echo ""Verifying \$pkg""
    sha256sum ""/mnt/c/Windows/Temp/\$pkg"" > ""/mnt/c/Windows/Temp/\$pkg.sha256""
done
"@
$verifyScript | Out-File -FilePath "C:\Windows\Temp\verify_packages.sh" -Encoding ASCII
wsl -u root -e bash -c "chmod +x /mnt/c/Windows/Temp/verify_packages.sh"