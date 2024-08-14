using SerialM.Business.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialM.Endpoint.WPF.Pages
{
    /// <summary>
    /// Interaction logic for NetworkTerminal.xaml
    /// </summary>
    public partial class NetworkTerminal : Page
    {
        private string _textboxFail = "TextBoxFailStyle",
            _textboxSuccess = "TextBoxSuccessStyle";

        public NetworkTerminal()
        {
            InitializeComponent();
        }

        private void InputIpTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsIpDigit();
        }
        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsNumeric();
        }

        private void InputIpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InputIpTextBox.Text.IsIPAddress(out _))
            {
                (sender as TextBox).Style = (Style)FindResource(_textboxSuccess);
            }
            else
            {
                (sender as TextBox).Style = (Style)FindResource(_textboxFail);
            }
        }

        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PortTextBox.Text.IsValidPort(out _))
            {
                (sender as TextBox).Style = (Style)FindResource(_textboxSuccess);
            }
            else
            {
                (sender as TextBox).Style = (Style)FindResource(_textboxFail);
            }
        }

    }
}
