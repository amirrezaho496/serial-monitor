using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SerialM.Endpoint.WPF.Validation
{
    public class IpAddressValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string ipAddress = value as string;

            if (string.IsNullOrEmpty(ipAddress))
                return false;

            return IPAddress.TryParse(ipAddress, out _);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
