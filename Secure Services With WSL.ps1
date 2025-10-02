$secureScript = @"
#!/bin/bash
# SecureVelocityServices.sh
for service in FileManagement ProcessScheduling Snapshot Security UIManager Supervisor Installer
do
    chown root:root /mnt/c/VelocityObjects/$service
    chmod 700 /mnt/c/VelocityObjects/$service
    echo "Secured $service in WSL" >> /mnt/c/Windows/Temp/velocity_security_log.txt
done
FLAG="secure_ps_$(date +%s)"
echo \$FLAG > /mnt/c/Windows/Temp/ps_flag.txt
CALLER=$(whoami)
if [[ "\$CALLER" != "root" ]]; then
    echo "Error: PowerShell scripts must be executed by root" > /mnt/c/Windows/Temp/ps_error.txt
    exit 1
fi
echo "PowerShell execution authorized for \$CALLER with flag \$FLAG" > /mnt/c/Windows/Temp/ps_auth.txt
"@
$secureScript | Out-File -FilePath "C:\Windows\Temp\SecureVelocityServices.sh" -Encoding ASCII