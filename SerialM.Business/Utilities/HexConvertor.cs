using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Utilities
{
    public static class HexConvertor
    {
        public static string ToHex(this string value)
        {
            // Convert the text to a byte array
            byte[] byteArray = Encoding.ASCII.GetBytes(value);

            // Convert each byte to hexadecimal
            string hexResult = BitConverter.ToString(byteArray).Replace("-", " ");

            return hexResult;
        }

        public static string FromHex(this string value)
        {
            // Remove any spaces from the input string
            value = value.Replace(" ", "");

            // Convert the hexadecimal string to a byte array
            byte[] byteArray = new byte[value.Length / 2];
            for (int i = 0; i < byteArray.Length; i++)
            {
                byteArray[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }

            // Convert the byte array to a text string using ASCII encoding
            string originalText = Encoding.ASCII.GetString(byteArray);

            return originalText;
        }

    }
}
