using System;
using System.Collections.Generic;
using System.Linq;
using OpenTap;

namespace OpenTAP.Docker;

public class ContainerInstance
{
    private TraceSource log;
    public string Name { get; set; }
    public string Image { get; set; }
    public List<Port> Ports { get; set; }
    public List<KeyValue> EnvironmentVariables { get; set; }
    public List<VolumeMapping> Volumes { get; set; }
    public List<KeyValue> Options { get; set; }
    public List<KeyValue> Arguments { get; set; }

    public bool PrintToLog { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public ContainerInstance(string name, string image, List<Port> ports, List<KeyValue> environmentVariables, List<VolumeMapping> volumes, List<KeyValue> options, List<KeyValue> arguments)
    {
        Name = name;
        Image = image;
        Ports = ports;
        EnvironmentVariables = environmentVariables;
        Volumes = volumes;
        Options = options;
        Arguments = arguments;
        
        log = Log.CreateSource(Name);
    }

    public void Pull()
    {
        ProcessHelper.StartNew("docker", $"pull {Image}", TapThread.Current.AbortToken,
            s => log.Info(s), 
            s => log.Error(s));
    }

    public void Start()
    {
        if (Options.Any(o => o.Name == "network"))
            throw new Exception("Option 'network' is not allowed.");
        
        // Start docker container
        var envArg = string.Join(" ", EnvironmentVariables.Select(e => $"-e \"{e.Name}\"=\"{e.Value}\""));
        var volArg = string.Join(" ", Volumes.Select(v => $"-v \"{v.Host}\":\"{v.Guest}\""));
        var portArg = string.Join(" ", Ports.Select(v => $"-p {v.Host}:{v.Guest}{(v.Type == PortType.TcpUdp ? "" : $"/{v.Type}")}"));
        var optionArg = string.Join(" ", Options.Select(o => $"--{o.Name} \"{o.Value}\""));
        var argsArg = string.Join(" ", Arguments.Select(a => $"--{a.Name} \"{a.Value}\""));
        var exitCode = ProcessHelper.StartNew("docker", $"run -dit --network opentap-service-network --name {Name} {optionArg} {envArg} {volArg} {portArg} {Image} {argsArg}", TapThread.Current.AbortToken,
            s => log.Info($"Started {Name} ({Image}): {s}"), 
            s => log.Error($"Starting {Name} ({Image}): {s}"), Timeout);

        if (exitCode != 0)
            throw new Exception("Failed to start container. See log for details.");

        // Attach to logs
        if (PrintToLog)
        {
            ProcessHelper.StartNew("docker", $"logs -f {Name}", TapThread.Current.AbortToken,
                s => log.Info(s), 
                s => log.Error(s), Timeout);
        }
    }
    
    public void Stop()
    {
        // Stop container
        ProcessHelper.StartNew("docker", $"stop {Name}", TapThread.Current.AbortToken,
            s => log.Info($"Stopping container: {s}"), 
            s => log.Error($"Error stopping container: {s}"), Timeout);
        // Remove container
        ProcessHelper.StartNew("docker", $"rm {Name}", TapThread.Current.AbortToken,
            s => log.Info($"Removing container: {s}"), 
            s => log.Error($"Error removing container: {s}"), Timeout);
    }
}