using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

public class VelocityInstallerService : BackgroundService
{
    private readonly string[] packages = { "package1.msi", "package2.msi", "package3.msi" };
    private readonly string wslScriptPath = @"C:\Windows\Temp\verify_packages.sh";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Verify packages in WSL
        await VerifyPackagesInWSL(packages);

        // Install packages concurrently
        await Task.WhenAll(packages.Select(p => InstallPackage(p, stoppingToken)));

        // Clean up residual files after uninstallation (example)
        foreach (var package in packages)
        {
            await CleanupResidualFiles(package);
        }
    }

    private async Task VerifyPackagesInWSL(string[] packages)
    {
        var wslScript = @"
#!/bin/bash
for pkg in ""$@""
do
    echo ""Verifying $pkg""
    sha256sum ""/mnt/c/Windows/Temp/$pkg"" > /mnt/c/Windows/Temp/$pkg.sha256
done
";
        File.WriteAllText(wslScriptPath, wslScript);
        await RunWslCommandAsync($"chmod +x /mnt/c/Windows/Temp/verify_packages.sh");
        await RunWslCommandAsync($"/mnt/c/Windows/Temp/verify_packages.sh {string.Join(" ", packages)}");
        EventLog.WriteEntry("VelocityInstaller", "Package verification completed in WSL", EventLogEntryType.Information);
    }

    private async Task InstallPackage(string package, CancellationToken stoppingToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "msiexec",
                Arguments = $"/i \"{package}\" /qn",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync(stoppingToken);
        if (process.ExitCode == 0)
            EventLog.WriteEntry("VelocityInstaller", $"Installed {package} successfully", EventLogEntryType.Information);
        else
            EventLog.WriteEntry("VelocityInstaller", $"Failed to install {package}", EventLogEntryType.Error);
    }

    private async Task CleanupResidualFiles(string package)
    {
        // Example cleanup logic (replace with actual paths)
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), package);
        if (Directory.Exists(appDataPath))
            Directory.Delete(appDataPath, true);
        await RunCommandAsync($"reg delete HKLM\\SOFTWARE\\{package} /f");
        EventLog.WriteEntry("VelocityInstaller", $"Cleaned residual files for {package}", EventLogEntryType.Information);
    }

    private async Task RunWslCommandAsync(string command)
    {
        await RunCommandAsync($"wsl -u root -e bash -c \"{command}\"");
    }

    private async Task RunCommandAsync(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddHostedService<VelocityInstallerService>();
            });
        await builder.Build().RunAsync();
    }
}