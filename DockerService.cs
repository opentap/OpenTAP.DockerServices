using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTap;

namespace OpenTAP.Docker;


public class DockerService : Resource, IService
{
    private TraceSource log = OpenTap.Log.CreateSource(nameof(DockerService));
    
    public int Order { get; set; }
    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }
    public string? Image { get; set; }
    public List<Port> Ports { get; set; } = new List<Port>();
    public List<KeyValue> EnvironmentVariables { get; set; } = new List<KeyValue>();
    public List<VolumeMapping> Volumes { get; set; } = new List<VolumeMapping>();
    // Options
    public List<KeyValue> Options { get; set; } = new List<KeyValue>();

    public override void Open()
    {
        if (Options.Any(o => o.Name == "network"))
            throw new Exception("Option 'network' is not allowed.");
        
        // Start docker container
        var varArg = string.Join(" ", EnvironmentVariables.Select(e => $"-e \"{e.Name}\"=\"{e.Value}\""));
        var volArg = string.Join(" ", Volumes.Select(v => $"-v \"{v.Host}\":\"{v.Guest}\""));
        var portArg = string.Join(" ", Ports.Select(v => $"-p {v.Host}:{v.Guest}{(v.Type == PortType.TcpUdp ? "" : $"/{v.Type}")}"));
        var optionArg = string.Join(" ", Options.Select(o => $"--{o.Name} \"{o.Value}\""));
        ProcessHelper.StartNew("docker", $"run -dit --network opentap-service-network --name {Name} {optionArg} {varArg} {volArg} {portArg} {Image}",
            s => log.Info($"Starting container: {s}"), 
            s => log.Error($"Error starting container: {s}"));
    }
    public override void Close()
    {
        // Stop container
        ProcessHelper.StartNew("docker", $"stop {Name}",
            s => log.Info($"Stopping container: {s}"), 
            s => log.Error($"Error stopping container: {s}"));
        // Remove container
        ProcessHelper.StartNew("docker", $"rm {Name}",
            s => log.Info($"Removing container: {s}"), 
            s => log.Error($"Error removing container: {s}"));
    }
}


public class DockerServiceMonitor : ComponentSettings<DockerServiceMonitor>, ITestPlanRunMonitor
{
    private TraceSource log = OpenTap.Log.CreateSource(nameof(DockerServiceMonitor));
    
    public void EnterTestPlanRun(TestPlanRun plan)
    {
        // Start docker network
        ProcessHelper.StartNew("docker", "network create opentap-service-network", 
            s => log.Info($"Creating network: {s}"), 
            s => log.Error($"Error creating network: {s}"));
        
        ServiceSettings.Current.OrderBy(s => s.Order).ToList().ForEach(s => s.Open());
    }

    public void ExitTestPlanRun(TestPlanRun plan)
    {
        ServiceSettings.Current.OrderBy(s => s.Order).ToList().ForEach(s => s.Close());
        
        // Stop network
        ProcessHelper.StartNew("docker", "network rm opentap-service-network",
            s => log.Info($"Stopping network: {s}"), 
            s => log.Error($"Error stopping network: {s}"));
    }
}