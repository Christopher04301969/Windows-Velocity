cd FileManagementService
dotnet add package Grpc.AspNetCore
cd ..\ProcessSchedulingService
dotnet add package Grpc.AspNetCore
cd ..\SnapshotService
dotnet add package Microsoft.Extensions.Hosting
cd ..\VelocityInstallerService
dotnet add package Microsoft.Extensions.Hosting
cd ..\SupervisorService
dotnet add package Microsoft.Extensions.Hosting
cd ..\SecurityService
dotnet add package Grpc.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
cd ..\VelocityDock
dotnet add package System.Windows.Forms