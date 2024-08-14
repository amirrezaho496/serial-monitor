using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerialM.Business.Utilities.Extensions
{
    public static class TextExtensions
    {
        public static bool IsNumeric(this string text)
        {
            Regex regex = new Regex("[^0-9]+"); // Regex that matches non-numeric text
            return !regex.IsMatch(text);
        }

        public static bool IsIPAddress(this string text, out IPAddress? iPAddress)
        {
            return IPAddress.TryParse(text, out iPAddress);
        }

        public static bool IsIpDigit(this string text)
        {
            Regex regex = new Regex(@"^[0-9.]+$"); // Regex that matches non-numeric text
            return regex.IsMatch(text);
        }
        public static bool IsValidPort(this string portString, out int port)
        {
            // Try to parse the string as an integer
            if (int.TryParse(portString, out port))
            {
                // Check if the parsed integer is within the valid port range
                if (port >= 0 && port <= 65535)
                {
                    return true;
                }
            }

            // If parsing fails or the number is out of range, return false
            return false;
        }

    }
}
