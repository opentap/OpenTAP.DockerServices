using OpenTap;

namespace OpenTAP.Docker;

public interface IService : IResource
{
    int Order { get; set; }
}


[SettingsGroup("Bench", false)]
[Display("Services", "Service Settings", null, -10000.0, false, null)]
public class ServiceSettings : ComponentSettingsList<ServiceSettings, IService>
{
}