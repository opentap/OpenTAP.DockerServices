using System;
using System.Collections.Generic;
using OpenTap;

namespace OpenTAP.Docker;

public class DockerStep : TestStep
{
    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }
    public string? Image { get; set; }
    public List<Port> Ports { get; set; } = new List<Port>();
    public List<KeyValue> EnvironmentVariables { get; set; } = new List<KeyValue>();
    public List<VolumeMapping> Volumes { get; set; } = new List<VolumeMapping>();
    public List<KeyValue> Options { get; set; } = new List<KeyValue>();
    public List<KeyValue> Arguments { get; set; } = new List<KeyValue>();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool PullOnPrePlanRun { get; set; } = true;
    
    private ContainerInstance container = null!;

    public override void PrePlanRun()
    {
        base.PrePlanRun();
        
        if (Image == null)
            throw new Exception("Image is not set.");
        
        container = new ContainerInstance(Name, Image, Ports, EnvironmentVariables, Volumes, Options, Arguments);
        container.Timeout = Timeout;
        
        if (PullOnPrePlanRun)
            container.Pull();
    }

    public override void Run()
    {
        container.Start();
        container.Stop();
    }
}