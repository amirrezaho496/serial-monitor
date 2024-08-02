using ModernWpf;
using SerialM.Business.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for SerialTerminal.xaml
    /// </summary>
    public partial class SerialTerminal : Page
    {
        private static SerialPort _serialPort;
        private Color? _default = ThemeManager.Current.AccentColor,
            _success = ThemeManager.Current.AccentColor,
            _fail = ThemeManager.Current.AccentColor,
            _info = ThemeManager.Current.AccentColor,
            _receive = ThemeManager.Current.AccentColor,
            _sent = ThemeManager.Current.AccentColor;
        private System.Timers.Timer _portCheckTimer;
        private string[] _lastKnownPorts;

        private bool _scrollToEnd = false, _hexText = false;

        public SerialTerminal()
        {
            InitializeComponent();
            InitializeSerialPort();
            LoadAvailablePorts();
            LoadPortSetting();
            InitializePortCheckTimer();
            Scroll_checkbox.IsChecked = true;
        }

        private void LoadPortSetting()
        {
            LoadBaudRates();
            LoadParity();
            LoadStopBits();
        }



        private void InitializeSerialPort()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void LoadAvailablePorts()
        {
            _lastKnownPorts = SerialPort.GetPortNames();
            PortComboBox.ItemsSource = _lastKnownPorts;
        }

        private void InitializePortCheckTimer()
        {
            _portCheckTimer = new System.Timers.Timer(1000); // Check every second
            _portCheckTimer.Elapsed += CheckPortChanges;
            _portCheckTimer.Start();
        }
        private void CheckPortChanges(object? sender, ElapsedEventArgs e)
        {
            string[] currentPorts = SerialPort.GetPortNames();
            if (!EqualArrays(_lastKnownPorts, currentPorts))
            {
                _lastKnownPorts = currentPorts;
                Dispatcher.Invoke(() => PortComboBox.ItemsSource = currentPorts);
            }
        }

        private bool EqualArrays(string[] array1, string[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }
            return true;
        }

        private void LoadBaudRates()
        {
            BaudRateComboBox.ItemsSource = new int[] { 300, 600, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200, 230400, 460800, 576000, 1040000, 1500000, 2000000 };
            BaudRateComboBox.SelectedIndex = 0; // Default to 9600
        }

        private void LoadParity()
        {
            string[] Parity = System.Enum.GetNames(typeof(Parity));
            ParityComboBox.ItemsSource = Parity;
            ParityComboBox.SelectedIndex = 0;
        }

        private void LoadStopBits()
        {
            string[] stopBits = System.Enum.GetNames(typeof(StopBits));
            StopBitComboBox.ItemsSource = stopBits;
            StopBitComboBox.SelectedIndex = 1;
        }

        private Parity GetSelectedParity()
        {
            return (Parity)Enum.Parse(typeof(Parity), ParityComboBox.SelectedItem.ToString());
        }
        private StopBits GetSelectedStopBits()
        {
            return (StopBits)Enum.Parse(typeof(StopBits), StopBitComboBox.SelectedItem.ToString());
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                ConnectButton.Content = "Connect";
                AppendTextToRichTextBox("Disconnected.\n", _fail);
            }
            else
            {
                _serialPort.PortName = PortComboBox.SelectedItem.ToString();
                _serialPort.BaudRate = (int)BaudRateComboBox.SelectedItem;
                _serialPort.Parity = GetSelectedParity(); 
                _serialPort.StopBits = GetSelectedStopBits();
                //_serialPort.Handshake = Handshake.
                _serialPort.Open();
                ConnectButton.Content = "Disconnect";
                AppendTextToRichTextBox("Connected.\n", _success);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadExisting();
            Dispatcher.Invoke(() => AppendTextToRichTextBox(data));
        }

        private async void HEX_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _hexText = (bool)HEX_checkbox.IsChecked;

            if (_hexText)
            {
                await TaskTextProccess(HexConvertor.ToHex);
            }
            else
            {
                await TaskTextProccess(HexConvertor.FromHex);
            }
        }

        private async Task TaskTextProccess(Func<string, string> textProccess)
        {
            await Task.Run(() =>
            {
                var txtrange = new TextRange(DataTextBox.Document.ContentStart, DataTextBox.Document.ContentEnd);
                Dispatcher.Invoke(() =>
                {
                    StringBuilder sb = new StringBuilder();
                    var lines = txtrange.Text.Replace("\n", "").Split('\r');

                    for (int i = 0; i < lines.Length-1; i++)
                    {
                        sb.AppendLine(textProccess(lines[i]));
                    }

                    txtrange.Text = sb.ToString();
                });
            });
        }

        private void Scroll_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _scrollToEnd = (bool)Scroll_checkbox.IsChecked;
            //Scroll_checkbox.IsChecked = _scrollToEnd;

            if (_scrollToEnd)
                DataTextBox.ScrollToEnd();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendData();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                SendData();
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                SendData();
                e.Handled = true;
            }
        }

        private void SendData()
        {
            if (_serialPort.IsOpen && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                _serialPort.WriteLine(InputTextBox.Text);
                AppendTextToRichTextBox($"Sent: {InputTextBox.Text.Replace("\n", "\n    ")}\n", _sent);
                InputTextBox.Clear();
            }
        }


        private void AppendTextToRichTextBox(string text, Color? color = null)
        {
            if (color == null)
                color = _default;

            if (_hexText)
             text = text.ToHex().Replace("0A", "\r");

            TextRange textRange = new TextRange(DataTextBox.Document.ContentEnd, DataTextBox.Document.ContentEnd)
            {
                Text = text
            };
            //textRange.ApplyPropertyValue(TextElement.ForegroundProperty, color);
            if (_scrollToEnd)
                DataTextBox.ScrollToEnd();
        }
    }
}
