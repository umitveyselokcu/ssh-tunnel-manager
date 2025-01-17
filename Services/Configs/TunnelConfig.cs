namespace SshTunnelManager.Services.Configs;

public class TunnelConfig
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string PemFileName { get; set; } = string.Empty;
    public int LocalPort { get; set; }
    public string RemoteHost { get; set; } = string.Empty;
    public int RemotePort { get; set; }
    public string BrowserUrl { get; set; } = string.Empty;
}