using System.ComponentModel;
using System.Net;
using System.Windows.Input;
using SshTunnelManager.Services.Commands;
using SshTunnelManager.Services.Configs;

namespace SshTunnelManager.Services;

public class TunnelViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public ICommand EditCommand { get; }
    public ICommand ToggleConnectionCommand { get; }
    public ICommand OpenBrowserCommand { get; }
    public ICommand RemoveConfigCommand { get; }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    private readonly TunnelConfig _config;
    private readonly Action<TunnelConfig, Action<bool>> _toggleConnection;
    public string IpAddress { get; set; } = string.Empty;
    public string PemFileName { get; set; } = string.Empty;
    public int LocalPort { get; set; }
    public string RemoteHost { get; set; } = string.Empty;
    public int RemotePort { get; set; }
    public TunnelViewModel(TunnelConfig config, Action<TunnelConfig, Action<bool>> toggleConnection, Action<TunnelConfig> openBrowser, Action<TunnelConfig> removeConfig, Action<TunnelConfig> editConfig)
    {
        _config = config;
        _toggleConnection = toggleConnection;

        Name = _config.Name;
        IpAddress = _config.IpAddress;
        PemFileName = _config.PemFileName;
        LocalPort = _config.LocalPort;
        RemoteHost = _config.RemoteHost;
        RemotePort = _config.RemotePort;
        ToggleConnectionCommand = new RelayCommand(() => _toggleConnection(_config, UpdateConnectionStatus));
        OpenBrowserCommand = new RelayCommand(() => openBrowser(_config));
        RemoveConfigCommand = new RelayCommand(() => removeConfig(_config));
        EditCommand = new RelayCommand(() => editConfig(_config));
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        IsConnected = isConnected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}