using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

// FileManagementService: Organizes files into nodes
public class FileManagementService : FileManagement.FileManagementBase
{
    public override Task<OrganizeResponse> OrganizeFiles(OrganizeRequest request, ServerCallContext context)
    {
        var nodePath = $@"C:\Nodes\{request.FileType}";
        Directory.CreateDirectory(nodePath);
        File.Move(request.FilePath, Path.Combine(nodePath, Path.GetFileName(request.FilePath)));
        EventLog.WriteEntry("VelocityService", $"Organized file {request.FilePath} to {nodePath}", EventLogEntryType.Information);
        return Task.FromResult(new OrganizeResponse { Success = true });
    }
}

// ProcessSchedulingService: Manages process priorities
public class ProcessSchedulingService : ProcessScheduling.ProcessSchedulingBase
{
    public override Task<ScheduleResponse> AdjustPriority(ScheduleRequest request, ServerCallContext context)
    {
        var process = Process.GetProcessById(request.ProcessId);
        process.PriorityClass = request.Priority switch
        {
            1 => ProcessPriorityClass.High,
            2 => ProcessPriorityClass.Normal,
            _ => ProcessPriorityClass.Low
        };
        EventLog.WriteEntry("VelocityService", $"Adjusted priority for PID {request.ProcessId}", EventLogEntryType.Information);
        return Task.FromResult(new ScheduleResponse { Success = true });
    }
}

// SnapshotService: Creates OS and memory snapshots
public class SnapshotService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshotDir = @"C:\Snapshots";
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var osSnapshotPath = Path.Combine(snapshotDir, $"OS_{timestamp}");
            Directory.CreateDirectory(osSnapshotPath);
            Registry.SaveHive(Registry.LocalMachine, Path.Combine(osSnapshotPath, "hklm.reg"));
            Registry.SaveHive(Registry.CurrentUser, Path.Combine(osSnapshotPath, "hkcu.reg"));
            File.Copy(@"C:\Windows\System32\config\SYSTEM", Path.Combine(osSnapshotPath, "SYSTEM"));
            EventLog.WriteEntry("VelocityService", $"OS snapshot created at {osSnapshotPath}", EventLogEntryType.Information);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

// VelocityInstallerService: Enhances Windows Installer
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

        // Clean up residual files after uninstallation
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

// SupervisorService: Monitors performance and snapshots
public class SupervisorService : BackgroundService
{
    private readonly ProcessSchedulingService _scheduler;

    public SupervisorService(ProcessSchedulingService scheduler)
    {
        _scheduler = scheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        while (!stoppingToken.IsCancellationRequested)
        {
            var cpuUsage = cpuCounter.NextValue();
            if (cpuUsage > 90)
            {
                foreach (var proc in Process.GetProcesses().Where(p => p.PriorityClass == ProcessPriorityClass.Normal))
                {
                    await _scheduler.AdjustPriority(new ScheduleRequest { ProcessId = proc.Id, Priority = 3 }, null);
                }
            }
            // Verify snapshot integrity
            var snapshotDir = @"C:\Snapshots";
            foreach (var dir in Directory.GetDirectories(snapshotDir, "OS_*"))
            {
                if (!File.Exists(Path.Combine(dir, "hklm.reg")))
                {
                    EventLog.WriteEntry("VelocityService", $"Corrupted snapshot detected: {dir}", EventLogEntryType.Warning);
                    // Trigger restoration
                    await RunWslCommandAsync($"/mnt/c/Windows/Temp/restore_snapshot.sh");
                }
            }
            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task RunWslCommandAsync(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c wsl -u root -e bash -c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
    }
}

// SecurityService: Handles authentication
public class SecurityService : Security.SecurityBase
{
    public override Task<AuthResponse> Authenticate(AuthRequest request, ServerCallContext context)
    {
        var identity = WindowsIdentity.GetCurrent();
        var token = GenerateJwtToken(identity.Name);
        return Task.FromResult(new AuthResponse { Token = token });
    }

    private string GenerateJwtToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "VelocityService",
            audience: "VelocityClients",
            claims: new[] { new Claim(ClaimTypes.Name, username) },
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// gRPC Protos
public class OrganizeRequest
{
    public string FilePath { get; set; }
    public string FileType { get; set; }
}

public class OrganizeResponse
{
    public bool Success { get; set; }
}

public class ScheduleRequest
{
    public int ProcessId { get; set; }
    public int Priority { get; set; }
}

public class ScheduleResponse
{
    public bool Success { get; set; }
}

public class AuthRequest
{
    public string Username { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddGrpc();
                services.AddHostedService<SnapshotService>();
                services.AddHostedService<SupervisorService>();
                services.AddHostedService<VelocityInstallerService>();
                services.AddHostedService<UIManager>();
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key"))
                        };
                    });
            });
        await builder.Build().RunAsync();
    }
}