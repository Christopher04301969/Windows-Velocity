# WindowsVelocityScript.ps1
# Run as Administrator
# Backup your system before running

# Enable and configure WSL
Write-Host "Enabling and configuring WSL..."
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -NoRestart
wsl --install -d Ubuntu -NoLaunch
wsl --set-default Ubuntu
Write-Host "WSL installed with Ubuntu. Run 'wsl' to complete setup."

# Optimize WSL
Write-Host "Optimizing WSL configuration..."
$wslConfig = @"
[wsl2]
memory=4GB
swap=0
localhostForwarding=true
"@
$wslConfig | Out-File -FilePath "$env:USERPROFILE\.wslconfig" -Encoding ASCII

# Create function objects
Write-Host "Classifying Windows functions into objects..."
$objectDir = "C:\VelocityObjects"
if (-not (Test-Path $objectDir)) {
    New-Item -Path $objectDir -ItemType Directory
}
$functionObjects = @{
    "FileManagement" = @("explorer.exe", "C:\Nodes")
    "ProcessScheduling" = @("taskmgr.exe", "C:\Windows\Temp\wsl_scheduler.sh")
    "Security" = @("C:\Windows\Temp\wsl_security.sh", "C:\Windows\Temp\ps_security.sh")
    "Maintenance" = @("C:\Windows\Temp\wsl_problem_handler.sh", "C:\Windows\Temp\snapshot_os.sh")
    "Installer" = @("C:\Windows\Temp\verify_packages.sh")
}
foreach ($obj in $functionObjects.Keys) {
    $objPath = Join-Path $objectDir $obj
    if (-not (Test-Path $objPath)) {
        New-Item -Path $objPath -ItemType Directory
    }
    foreach ($item in $functionObjects[$obj]) {
        if (Test-Path $item) {
            Copy-Item -Path $item -Destination $objPath -Force
        }
    }
}

# Optimize boot time
Write-Host "Enabling Fast Startup..."
powercfg /hibernate on
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Power" -Name "HiberbootEnabled" -Value 1

Write-Host "Disabling unnecessary startup programs..."
$startupItems = Get-CimInstance Win32_StartupCommand | Where-Object { $_.User -eq "All Users" -or $_.User -eq $env:USERNAME }
foreach ($item in $startupItems) {
    if ($item.Command -notlike "*Windows*") {
        Disable-CimInstance -InputObject $item -ErrorAction SilentlyContinue
        Write-Host "Disabled startup item: $($item.Name)"
    }
}

# Optimize memory
Write-Host "Configuring memory for low-hardware systems..."
$os = Get-CimInstance Win32_OperatingSystem
$totalMemory = $os.TotalVisibleMemorySize / 1MB
if ($totalMemory -lt 8) {
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -Name "DisablePagingExecutive" -Value 1
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" -Name "LargeSystemCache" -Value 0
}
wmic computersystem where name="%computername%" set AutomaticManagedPagefile=False
wmic pagefileset delete
Write-Host "pagefile.sys removed."

# Disable memory compression
Write-Host "Disabling memory compression..."
Stop-Process -Name "Memory Compression" -Force -ErrorAction SilentlyContinue
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\SysMain" -Name "Start" -Value 4

# Fix Explorer crashes
Write-Host "Resetting Windows Explorer settings..."
Stop-Process -Name "explorer" -Force
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer" -Recurse -ErrorAction SilentlyContinue
sfc /scannow
DISM /Online /Cleanup-Image /RestoreHealth

# Disable Accessibility Command Prompt
Write-Host "Disabling Accessibility Command Prompt..."
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\utilman.exe" -Name "Debugger" -Value $null

# Node-based file handling
Write-Host "Creating node-based file handling structure..."
$objectFolder = "C:\Nodes"
if (-not (Test-Path $objectFolder)) {
    New-Item -Path $objectFolder -ItemType Directory
}
$nodeRules = @{
    "Documents" = @("*.docx", "*.pdf", "*.txt")
    "Media" = @("*.jpg", "*.png", "*.mp4", "*.mp3")
    "Code" = @("*.py", "*.cs", "*.js")
}
foreach ($category in $nodeRules.Keys) {
    $categoryPath = Join-Path $objectFolder $category
    if (-not (Test-Path $categoryPath)) {
        New-Item -Path $categoryPath -ItemType Directory
    }
}
$dropHandlerScript = @"
Add-Type -AssemblyName System.Windows.Forms
\$dropFolder = '$objectFolder'
\$form = New-Object Windows.Forms.Form
\$form.Text = 'Node-Based File Handler'
\$form.AllowDrop = \$true
\$form.Add_DragEnter({
    if (\$_.Data.GetDataPresent([Windows.Forms.DataFormats]::FileDrop)) {
        \$_.Effect = [Windows.Forms.DragDropEffects]::Move
    }
})
\$form.Add_DragDrop({
    \$files = \$_.Data.GetData([Windows.Forms.DataFormats]::FileDrop)
    foreach (\$file in \$files) {
        \$ext = [System.IO.Path]::GetExtension(\$file).ToLower()
        switch -Wildcard (\$ext) {
            '*.docx' { Move-Item \$file -Destination '$objectFolder\Documents' }
            '*.pdf'  { Move-Item \$file -Destination '$objectFolder\Documents' }
            '*.txt'  { Move-Item \$file -Destination '$objectFolder\Documents' }
            '*.jpg'  { Move-Item \$file -Destination '$objectFolder\Media' }
            '*.png'  { Move-Item \$file -Destination '$objectFolder\Media' }
            '*.mp4'  { Move-Item \$file -Destination '$objectFolder\Media' }
            '*.mp3'  { Move-Item \$file -Destination '$objectFolder\Media' }
            '*.py'   { Move-Item \$file -Destination '$objectFolder\Code' }
            '*.cs'   { Move-Item \$file -Destination '$objectFolder\Code' }
            '*.js'   { Move-Item \$file -Destination '$objectFolder\Code' }
            default  { Write-Host 'Unsupported file type: \$file' }
        }
    }
})
\$form.ShowDialog()
"@
$dropHandlerScript | Out-File -FilePath "C:\Windows\Temp\NodeDropHandler.ps1" -Encoding ASCII
Start-Process powershell -ArgumentList "-File C:\Windows\Temp\NodeDropHandler.ps1" -NoNewWindow

# Apply VR theme
Write-Host "Applying hybrid VR desktop theme..."
$themeRegPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
Set-ItemProperty -Path $themeRegPath -Name "AppsUseLightTheme" -Value 0
Set-ItemProperty -Path $themeRegPath -Name "SystemUsesLightTheme" -Value 0
$vrThemeScript = @"
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class DwmApi {
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
}
"@
\$hwnd = (Get-Process -Name explorer).MainWindowHandle
\$attr = 19 # DWMWA_USE_HOSTBACKDROPBRUSH
\$attrValue = 1
[DwmApi]::DwmSetWindowAttribute(\$hwnd, \$attr, [ref]\$attrValue, 4)
"@
$vrThemeScript | Out-File -FilePath "C:\Windows\Temp\ApplyVRTheme.ps1" -Encoding ASCII
Start-Process powershell -ArgumentList "-File C:\Windows\Temp\ApplyVRTheme.ps1" -Verb RunAs

Write-Host "Windows Velocity setup complete. Reboot required."