using System.Globalization;
using System.Windows.Data;

namespace SshTunnelManager.Services.Helpers;

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