using SerialM.Business.Utilities.Extensions;
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
    public class PortValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string port = value as string;

            if (string.IsNullOrEmpty(port))
                return false;

            return port.IsValidPort(out _);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
