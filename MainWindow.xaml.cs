using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Renci.SshNet;
using SshTunnelManager.Services;
using SshTunnelManager.Services.Configs;

namespace SshTunnelManager
{
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "tunnels.json";
        private const string GlobalConfigFileName = "globalconfig.json";

        private readonly List<TunnelConfig> _configurations;
        private readonly Dictionary<string, SshClient> _activeConnections = new();
        private GlobalConfig _globalConfig;
        private bool _showDetails = true;

        public MainWindow()
        {
            InitializeComponent();
            _globalConfig = LoadGlobalConfig();
            _configurations = LoadConfigurations();
            ValidatePemFiles();
            UpdateTunnelTable();
            PemDirectoryPathText.Text = _globalConfig.PemDirectoryPath; // Display the PEM directory
            Closing += MainWindow_Closing;
        }

        private List<TunnelConfig> LoadConfigurations()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (!File.Exists(configPath))
                return new List<TunnelConfig>();

            try
            {
                var jsonData = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<List<TunnelConfig>>(jsonData) ?? new List<TunnelConfig>();
            }
            catch
            {
                MessageBox.Show("Failed to load configurations. The file might be corrupted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<TunnelConfig>();
            }
        }

        private GlobalConfig LoadGlobalConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GlobalConfigFileName);
            if (!File.Exists(configPath))
            {
                var defaultConfig = new GlobalConfig();
                SaveGlobalConfig(defaultConfig);
                return defaultConfig;
            }

            var configJson = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<GlobalConfig>(configJson) ?? new GlobalConfig();
        }

        private void SaveConfigurations()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            var jsonData = JsonSerializer.Serialize(_configurations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, jsonData);
        }

        private void SaveGlobalConfig(GlobalConfig config)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GlobalConfigFileName);
            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, configJson);
        }

        private void ValidatePemFiles()
        {
            var missingFiles = _configurations
                .Where(config => !File.Exists(Path.Combine(_globalConfig.PemDirectoryPath, config.PemFileName)))
                .Select(config => Path.Combine(_globalConfig.PemDirectoryPath, config.PemFileName))
                .ToList();

            if (missingFiles.Any())
            {
                MessageBox.Show($"The following PEM files are missing:\n{string.Join("\n", missingFiles)}",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private List<TunnelViewModel> GenerateViewModels(List<TunnelConfig> configs)
        {
            var viewModels = new List<TunnelViewModel>();

            foreach (var config in configs)
            {
                viewModels.Add(new TunnelViewModel(config, ToggleConnection, OpenBrowser, RemoveConfiguration, EditTunnel_Click));
            }

            return viewModels;
        }

        private void ToggleConnection(TunnelConfig config, Action<bool> updateConnectionStatus)
        {
            if (_activeConnections.ContainsKey(config.Name))
            {
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
                    var pemFilePath = Path.Combine(_globalConfig.PemDirectoryPath, config.PemFileName);
                    if (!File.Exists(pemFilePath))
                        throw new FileNotFoundException("PEM file not found", pemFilePath);

                    using var privateKeyFile = new PrivateKeyFile(pemFilePath);
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

        private void RemoveConfiguration(TunnelConfig config)
        {
            if (_configurations.Remove(config))
            {
                SaveConfigurations();
                UpdateTunnelTable();
                MessageBox.Show($"Configuration for {config.Name} removed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to remove configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTunnelTable()
        {
            if (_showDetails)
            {
                TunnelTable.Columns[1].Visibility = Visibility.Visible; // IP Address
                TunnelTable.Columns[2].Visibility = Visibility.Visible; // Local Port
                TunnelTable.Columns[3].Visibility = Visibility.Visible; // Remote Host
                TunnelTable.Columns[4].Visibility = Visibility.Visible; // Remote Port
                TunnelTable.Columns[5].Visibility = Visibility.Visible; // PemFile
                TunnelTable.Columns[9].Visibility = Visibility.Visible; // Update
            }
            else
            {
                TunnelTable.Columns[1].Visibility = Visibility.Collapsed; // IP Address
                TunnelTable.Columns[2].Visibility = Visibility.Collapsed; // Local Port
                TunnelTable.Columns[3].Visibility = Visibility.Collapsed; // Remote Host
                TunnelTable.Columns[4].Visibility = Visibility.Collapsed; // Remote Port
                TunnelTable.Columns[5].Visibility = Visibility.Collapsed; // PemFile
                TunnelTable.Columns[9].Visibility = Visibility.Collapsed; // Update
            }

            TunnelTable.ItemsSource = GenerateViewModels(_configurations);
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

        private void ImportConfigurations(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Import Tunnel Configurations"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var importedJson = File.ReadAllText(openFileDialog.FileName);
                var importedConfigs = JsonSerializer.Deserialize<List<TunnelConfig>>(importedJson) ?? new List<TunnelConfig>();

                _configurations.AddRange(importedConfigs);
                SaveConfigurations();
                UpdateTunnelTable();
                MessageBox.Show("Configurations imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SetPemDirectoryPath(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select PEM File Directory"
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _globalConfig.PemDirectoryPath = folderDialog.FileName;
                SaveGlobalConfig(_globalConfig);
                PemDirectoryPathText.Text = _globalConfig.PemDirectoryPath; // Update displayed PEM directory
                MessageBox.Show("PEM directory path updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddTunnel_Click(object sender, RoutedEventArgs e)
        {
            var addEditWindow = new AddEditTunnelWindow();
            if (addEditWindow.ShowDialog() == true)
            {
                _configurations.Add(addEditWindow.TunnelConfig);
                SaveConfigurations();
                UpdateTunnelTable();
            }
        }

        private void EditTunnel_Click(TunnelConfig config)
        {
            var addEditWindow = new AddEditTunnelWindow(config);
            if (addEditWindow.ShowDialog() == true)
            {
                SaveConfigurations();
                UpdateTunnelTable();
            }
        }

        private void ToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            _showDetails = !_showDetails;
            UpdateTunnelTable();
        }
    }
}
