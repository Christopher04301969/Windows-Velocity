# ApplyBlur.ps1
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class DwmApi {
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}
"@
while ($true) {
    $fgWindow = [DwmApi]::GetForegroundWindow()
    $processes = Get-Process | Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowHandle -ne $fgWindow -and $_.ProcessName -ne 'powershell' }
    foreach ($proc in $processes) {
        $attr = 19 # DWMWA_USE_HOSTBACKDROPBRUSH
        $attrValue = 1
        [DwmApi]::DwmSetWindowAttribute($proc.MainWindowHandle, $attr, [ref]$attrValue, 4)
    }
    Start-Sleep -Milliseconds 500
}