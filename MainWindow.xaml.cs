using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Renci.SshNet;

namespace SshTunnelManager
{
    public class TunnelConfig
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string PemFilePath { get; set; } = string.Empty;
        public int LocalPort { get; set; }
        public string RemoteHost { get; set; } = string.Empty;
        public int RemotePort { get; set; }
        public string BrowserUrl { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private readonly List<TunnelConfig> _configurations;
        private readonly Dictionary<string, SshClient> _activeConnections = new();

        public MainWindow()
        {
            InitializeComponent();
            _configurations = LoadConfigurations();
            TunnelTable.ItemsSource = GenerateViewModels(_configurations);
            Closing += MainWindow_Closing;
        }

        private List<TunnelConfig> LoadConfigurations()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tunnels.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Configuration file not found", configPath);

            var configJson = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<List<TunnelConfig>>(configJson) ?? new List<TunnelConfig>();
        }

        private List<TunnelViewModel> GenerateViewModels(List<TunnelConfig> configs)
        {
            var viewModels = new List<TunnelViewModel>();

            foreach (var config in configs)
            {
                viewModels.Add(new TunnelViewModel(config, ToggleConnection, OpenBrowser));
            }

            return viewModels;
        }

        private void ToggleConnection(TunnelConfig config, Action<bool> updateConnectionStatus)
        {
            if (_activeConnections.ContainsKey(config.Name))
            {
                // Disconnect
                try
                {
                    _activeConnections[config.Name].Disconnect();
                    _activeConnections.Remove(config.Name);
                    updateConnectionStatus(false);

                    MessageBox.Show($"Disconnected from {config.Name}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to disconnect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                try
                {
                    using var privateKeyFile = new PrivateKeyFile(config.PemFilePath);
                    var connectionInfo = new ConnectionInfo(
                        config.IpAddress,
                        "ubuntu",
                        new PrivateKeyAuthenticationMethod("ubuntu", privateKeyFile)
                    );

                    var client = new SshClient(connectionInfo);
                    client.Connect();

                    var portForward = new ForwardedPortLocal(
                        "127.0.0.1",
                        (uint)config.LocalPort,
                        config.RemoteHost,
                        (uint)config.RemotePort
                    );

                    client.AddForwardedPort(portForward);
                    portForward.Start();

                    _activeConnections[config.Name] = client;
                    updateConnectionStatus(true);

                    MessageBox.Show($"Connected to {config.Name}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenBrowser(TunnelConfig config)
        {
            if (!_activeConnections.ContainsKey(config.Name))
            {
                MessageBox.Show("Connection is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(config.BrowserUrl))
            {
                MessageBox.Show("No browser URL specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = config.BrowserUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            foreach (var client in _activeConnections.Values)
            {
                try
                {
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error disconnecting client: {ex.Message}");
                }
            }
        }
    }

    public class TunnelViewModel : INotifyPropertyChanged
    {
        public string Name { get; }
        public ICommand ToggleConnectionCommand { get; }
        public ICommand OpenBrowserCommand { get; }

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

        public TunnelViewModel(TunnelConfig config, Action<TunnelConfig, Action<bool>> toggleConnection, Action<TunnelConfig> openBrowser)
        {
            _config = config;
            _toggleConnection = toggleConnection;

            Name = _config.Name;
            ToggleConnectionCommand = new RelayCommand(() => _toggleConnection(_config, UpdateConnectionStatus));
            OpenBrowserCommand = new RelayCommand(() => openBrowser(_config));
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

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class BoolToConnectDisconnectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isConnected && isConnected ? "Disconnect" : "Connect";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
