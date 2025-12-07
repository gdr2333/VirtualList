using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VirtualList.Services;

public class WebDavServerService : IHostedService
{
    private Process _serverProcess;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string addr = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "Native/WebDAV-Linux-AMD64/webdav",
                Architecture.Arm64 => "Native/WebDAV-Linux-ARM64/webdav",
                _ => throw new NotImplementedException(),
            },
            PlatformID.Win32NT => RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "Native/WebDAV-Windows-AMD64/webdav",
                Architecture.Arm64 => "Native/WebDAV-Windows-ARM64/webdav",
                _ => throw new NotImplementedException(),
            },
            _ => throw new NotImplementedException(),
        };
        _serverProcess = new Process();
        _serverProcess.StartInfo.FileName = addr;
        _serverProcess.StartInfo.Arguments = "--config DavServerConfig-AutoGen.yaml";
        _serverProcess.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _serverProcess.Close();
        await _serverProcess.WaitForExitAsync(cancellationToken);
    }
}
