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
}