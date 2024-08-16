using SerialM.Business.Utilities.Extensions;
using SerialM.Endpoint.WPF.Data;
using System;
using System.Collections.Generic;
using System.IO.Ports;
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
        #region Data
        private const string _disconnectBtnText = "Disconnect";
        private const string _connectBtnText = "Connect";
        private const string _ListenBtnText = "Listen";
        private static SerialPort _serialPort;
        private System.Timers.Timer _portCheckTimer;
        private string[] _lastKnownPorts;

        TerminalPageData _pageData;
        #endregion

        public NetworkTerminal()
        {
            InitializeComponent();
        }

        #region Validation Events

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
                (sender as TextBox).Style = (Style)FindResource(TerminalPageData.TEXTBOX_SUCCESS_RESOURCEKEY);
            }
            else
            {
                (sender as TextBox).Style = (Style)FindResource(TerminalPageData.TEXTBOX_FAIL_RESOURCEKEY);
            }
        }


        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PortTextBox.Text.IsValidPort(out _))
            {
                (sender as TextBox).Style = (Style)FindResource(TerminalPageData.TEXTBOX_SUCCESS_RESOURCEKEY);
            }
            else
            {
                (sender as TextBox).Style = (Style)FindResource(TerminalPageData.TEXTBOX_FAIL_RESOURCEKEY);
            }
        }

        #endregion

        #region Click Event
        private void AddNewSendItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AutoRunBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ToHex_button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearHex_button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Clear_button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SendSavedCommand_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region Preview Events
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }
        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        #endregion

        #region Change Events

        private void HEX_checkbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Scroll_checkbox_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void LogSplitter_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
        #endregion

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
