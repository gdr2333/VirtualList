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
                Architecture.X64 => "webdav-amd64-linux",
                Architecture.Arm64 => "webdav-arm64-linux",
                _ => throw new NotImplementedException(),
            },
            PlatformID.Win32NT => RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "webdav-amd64-win.exe",
                Architecture.Arm64 => "webdav-arm64-win.exe",
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
        if(!_serverProcess.HasExited)
            await _serverProcess.WaitForExitAsync(cancellationToken);
    }
}
