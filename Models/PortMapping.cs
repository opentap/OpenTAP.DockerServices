namespace OpenTAP.Docker;

public enum PortType
{
    TcpUdp,
    Tcp,
    Udp
}

public class Port
{
    public int Host { get; set; }
    public int Guest { get; set; }
    public PortType Type { get; set; }
}