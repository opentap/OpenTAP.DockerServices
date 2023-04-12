using System.Linq;
using OpenTap;

namespace OpenTAP.Docker;

public class DockerServiceMonitor : ComponentSettings<DockerServiceMonitor>, ITestPlanRunMonitor
{
    private TraceSource log = OpenTap.Log.CreateSource(nameof(DockerServiceMonitor));
    
    public void EnterTestPlanRun(TestPlanRun plan)
    {
        // TODO: Check if any steps or services are using docker
        
        // Start docker network
        ProcessHelper.StartNew("docker", "network create opentap-service-network", TapThread.Current.AbortToken,
            s => log.Info($"Creating network: {s}"), 
            s => log.Error($"Error creating network: {s}"));
        
        ServiceSettings.Current.OrderBy(s => s.Order).ToList().ForEach(s => s.Open());
    }

    public void ExitTestPlanRun(TestPlanRun plan)
    {
        ServiceSettings.Current.OrderByDescending(s => s.Order).ToList().ForEach(s => s.Close());
        
        // Stop network
        ProcessHelper.StartNew("docker", "network rm opentap-service-network", TapThread.Current.AbortToken,
            s => log.Info($"Stopping network: {s}"), 
            s => log.Error($"Error stopping network: {s}"));
    }
}