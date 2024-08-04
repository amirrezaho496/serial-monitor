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
        //private string _default = "DefaultTextColor",
        //    _success = "SuccessColor",
        //    _fail = "FailColor",
        //    _info = "InfoColor",
        //    _receive = "ReceiveColor",
        //    _sent = "SentColor";

        private string _default = "DefaultTextStyle",
            _success = "SuccessStyle",
            _fail = "FailStyle",
            _info = "InfoStyle",
            _receive = "ReceiveStyle",
            _sent = "SentStyle";


        private System.Timers.Timer _portCheckTimer;
        private string[] _lastKnownPorts;

        private bool _scrollToEnd = false, _hexText = false;

        private List<Run> _textRanges = new();
        private Paragraph mainParagraph = new();

        public List<Run> TextRanges
        {
            get => _textRanges;
        }

        public SerialTerminal()
        {
            InitializeComponent();
            InitializeSerialPort();
            LoadAvailablePorts();
            LoadPortSetting();
            InitializePortCheckTimer();
            Scroll_checkbox.IsChecked = true;
            ThemeManager.Current.ActualApplicationThemeChanged += RefreshColors;
            DataTextBox.Document.Blocks.Clear();
            DataTextBox.Document.Blocks.Add(mainParagraph);
        }

        private void LoadPortSetting()
        {
            LoadBaudRates();
            LoadParity();
            LoadStopBits();
        }

        private void RefreshColors(ThemeManager themeManager, object e)
        {
            //_success = new SolidColorBrush((ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light) ? Colors.DarkGreen : Colors.LightGreen);
            //_fail = new SolidColorBrush((ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light) ? Colors.DarkRed : Colors.MediumVioletRed);
            //_info = new SolidColorBrush((ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light) ? Colors.DarkBlue : Colors.LightBlue);
            //_receive = new SolidColorBrush((ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light) ? Colors.Black : Colors.White);
            //_sent = new SolidColorBrush((ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light) ? Colors.GreenYellow : Colors.GreenYellow);
            //_default = (themeManager.AccentColor != null) ? new SolidColorBrush((Color)themeManager.AccentColor) : _receive;
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

        private void HEX_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _hexText = (bool)HEX_checkbox.IsChecked;

            if (_hexText)
            {
                TaskTextProccess(HexConvertor.ToHex);
            }
            else
            {
                TaskTextProccess(HexConvertor.FromHex);
            }
        }

        private void TaskTextProccess(Func<string, string> textProccess)
        {
            Task.Run(() =>
            {
                //var txtrange = new TextRange(DataTextBox.Document.ContentStart, DataTextBox.Document.ContentEnd);
                Dispatcher.Invoke(() =>
                {
                    //StringBuilder sb = new StringBuilder();
                    //var lines = txtrange.Text.Replace("\n", "").Split('\r');

                    //for (int i = 0; i < lines.Length-1; i++)
                    //{
                    //    sb.AppendLine(textProccess(lines[i]));
                    //}

                    //txtrange.Text = sb.ToString();
                    //mainSbar.Minimom = 0;
                    foreach (var txt in TextRanges)
                    {
                        if (_hexText)
                            txt.Text = textProccess(txt.Text).Replace("0A", "\r");
                        else
                            txt.Text = textProccess(txt.Text.Replace("\r", "0A"));
                    }
                });
            });
        }

        private void ToHex_button_Click(object sender, RoutedEventArgs e)
        {
            var selected = DataTextBox.Selection.Text;
            var hexSelected = "";
            if (!_hexText)
            {
                hexSelected = selected.ToHex();
            }
            else
            {
                hexSelected = selected;
                selected = hexSelected.FromHex();
            }



            new TextRange(HexDataTextBox.Document.ContentEnd, HexDataTextBox.Document.ContentEnd)
            {
                Text = selected + ':' + hexSelected + '\n'
            };
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


        private void AppendTextToRichTextBox(string text, string colorResourceKey = "")
        {

            if (_hexText)
                text = text.ToHex().Replace("0A", "\r");

            var start = DataTextBox.Document.ContentEnd;
            Run run = new Run(text, DataTextBox.Document.ContentEnd);

            //var run = new Run(text, DataTextBox.Document.ContentEnd);
            // Convert the Color to a SolidColorBrush
            //SolidColorBrush brush = new SolidColorBrush((Color)color);
            //textRange.ApplyPropertyValue(TextElement.ForegroundProperty, color);


            if (string.IsNullOrEmpty(colorResourceKey))
                colorResourceKey = _default;

            var span = new Span(run, start);
            span.Style = (Style)FindResource(colorResourceKey);
            //if (!string.IsNullOrEmpty(colorResourceKey))
            //{
            //    textRange.ApplyPropertyValue(TextElement.ForegroundProperty, App.Current.Resources[colorResourceKey]);
            //}

            //DataTextBox.Document.Blocks.Add(new Paragraph(span));
            mainParagraph.Inlines.Add(span);

            _textRanges.Add(run);

            if (_scrollToEnd)
                DataTextBox.ScrollToEnd();
        }
    }
}
