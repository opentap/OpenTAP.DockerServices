using System;
using System.Collections.Generic;
using OpenTap;

namespace OpenTAP.Docker;

public class DockerService : Resource, IService
{
    private ContainerInstance? container;

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
    public List<KeyValue> Options { get; set; } = new List<KeyValue>();
    public List<KeyValue> Arguments { get; set; } = new List<KeyValue>();

    public override void Open()
    {
        if (Image == null)
            throw new Exception("Image is not set.");

        container = new ContainerInstance(Name, Image, Ports, EnvironmentVariables, Volumes, Options, Arguments);
        container.Timeout = TimeSpan.MaxValue;
        container.WaitForExit = false;
        Log.Info($"Starting service: {Name}");
        container.Start();
    }
    public override void Close()
    {
        Log.Info($"Stopping service: {Name}");
        container?.Stop();
    }
}