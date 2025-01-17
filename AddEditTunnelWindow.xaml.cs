using System.Windows;
using SshTunnelManager.Services.Configs;

namespace SshTunnelManager
{
    public partial class AddEditTunnelWindow : Window
    {
        public TunnelConfig TunnelConfig { get; private set; }
        private bool _isEditMode;

        public AddEditTunnelWindow(TunnelConfig tunnelConfig = null)
        {
            InitializeComponent();

            if (tunnelConfig != null)
            {
                TunnelConfig = tunnelConfig;
                _isEditMode = true;
                PopulateFields();
            }
            else
            {
                TunnelConfig = new TunnelConfig();
            }
        }

        private void PopulateFields()
        {
            NameTextBox.Text = TunnelConfig.Name;
            IpAddressTextBox.Text = TunnelConfig.IpAddress;
            PemFileNameTextBox.Text = TunnelConfig.PemFileName;
            LocalPortTextBox.Text = TunnelConfig.LocalPort.ToString();
            RemoteHostTextBox.Text = TunnelConfig.RemoteHost;
            RemotePortTextBox.Text = TunnelConfig.RemotePort.ToString();
            BrowserUrlTextBox.Text = TunnelConfig.BrowserUrl;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate and save
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(IpAddressTextBox.Text) ||
                string.IsNullOrWhiteSpace(PemFileNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(RemoteHostTextBox.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(LocalPortTextBox.Text, out int localPort) ||
                !int.TryParse(RemotePortTextBox.Text, out int remotePort))
            {
                MessageBox.Show("Invalid port number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TunnelConfig.Name = NameTextBox.Text;
            TunnelConfig.IpAddress = IpAddressTextBox.Text;
            TunnelConfig.PemFileName = PemFileNameTextBox.Text;
            TunnelConfig.LocalPort = localPort;
            TunnelConfig.RemoteHost = RemoteHostTextBox.Text;
            TunnelConfig.RemotePort = remotePort;
            TunnelConfig.BrowserUrl = BrowserUrlTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
