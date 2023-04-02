using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace OpenTAP.Docker;

public class Maintenance
{
    public static void Prune(CancellationToken cancellationToken)
    {
        // docker system prune -f
        ProcessHelper.StartNew("docker", "system prune -f", cancellationToken, s => {}, s => {});
    }

    public static void CleanVolumes(CancellationToken cancellationToken)
    {
        // docker system prune --volumes -f
        ProcessHelper.StartNew("docker", "system prune --volumes -f", cancellationToken, s => {}, s => {});
    }

    public static void CleanImages(string filter, CancellationToken cancellationToken)
    {
        var images = new List<string>();
        ProcessHelper.StartNew("docker", "system prune --volumes -f", cancellationToken, s =>
        {
            images.Add(s);
        }, s =>
        {
            // TODO: Handle error
        });

        var imagesToRemove = images.Where(i => Regex.IsMatch(i, filter));
        foreach (var image in imagesToRemove)
        {
            ProcessHelper.StartNew("docker", $"image rm {image}", cancellationToken, s =>
            {
                
            }, s =>
            {
                // TODO: Handle error
            });
        }

        // docker image list | grep alpha | awk '{print $3}' | xarg -n1 docker rmi
    }

    public static void StopAllContainers(CancellationToken cancellationToken)
    {
        bool error = false;
        var containers = new List<string>();
        ProcessHelper.StartNew("docker", "ps -q", cancellationToken, s => { containers.Add(s); }, s => { error = true; });
        if (error)
            throw new Exception("Failed to get list of containers.");
        
        foreach (var container in containers)
            ProcessHelper.StartNew("docker", $"stop {container}", cancellationToken, s => { }, s => { });
    }

    public static void RemoveAllContainers(CancellationToken cancellationToken)
    {
        bool error = false;
        var containers = new List<string>();
        ProcessHelper.StartNew("docker", "ps -aq", cancellationToken, s => { containers.Add(s); }, s => { error = true; });
        if (error)
            throw new Exception("Failed to get list of containers.");
        
        foreach (var container in containers)
            ProcessHelper.StartNew("docker", $"rm {container}", cancellationToken, s => { }, s => { });
    }
}