using ModernWpf;
using SerialM.Business.Extensions;
using SerialM.Business.Utilities;
using SerialM.Endpoint.WPF.Models;
using SerialM.Endpoint.WPF.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        #region Data
        private const string _TimeResourceKey = "TimeStyle";
        private const string _disconnectBtnText = "Disconnect";
        private const string _connectBtnText = "Connect";
        private static SerialPort _serialPort;
        private Task _autoRunTask;
        private CancellationTokenSource _cancellationTokenSource;

        private ObservableCollection<SendListViewItem> _sendListViewItems;
        public ObservableCollection<SendListViewItem> SendItems
        {
            get => _sendListViewItems;
            set
            {
                this._sendListViewItems = value;
                ListView.ItemsSource = this._sendListViewItems;
            }
        }
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
        private int _hex_limit = 1000;

        private List<TextboxItem> _textRanges = new();
        private Paragraph mainParagraph = new();

        public List<TextboxItem> TextRanges
        {
            get => _textRanges;
        }
        #endregion

        public SerialTerminal()
        {
            InitializeComponent();

            InitializeSerialPort();
            LoadAvailablePorts();
            LoadPortSetting();
            InitializePortCheckTimer();
            Scroll_checkbox.IsChecked = true;
            InitializeTextBox();
            InitializeListView();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            SavePage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPage();
        }



        #region Methods

        private void InitializeListView()
        {
            SendItems = new();
            SendItems.Add(new SendListViewItem());
            ListView.ItemsSource = SendItems;
        }

        private void InitializeTextBox()
        {
            DataTextBox.Document.Blocks.Clear();
            DataTextBox.Document.Blocks.Add(mainParagraph);
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
            _serialPort.ErrorReceived += SerialPort_ErrorReceived;
            _serialPort.Disposed += SerialPort_Disposed;
        }

        private void SerialPort_Disposed(object? sender, EventArgs e)
        {
            _serialPort.Container?.Dispose();
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            PortClose();
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


        private async void TaskTextProccess(Func<string, string> textProccess)
        {
            await Task.Run(() =>
            {
                SetSBar(TextRanges.Count, 0, 0);

                //var txtrange = new TextRange(DataTextBox.Document.ContentStart, DataTextBox.Document.ContentEnd);
                int last = TextRanges.Count - 2000;
                if (last < 0) last = 0;
                for (int i = TextRanges.Count - 1; i >= last; i--)
                {
                    var txt = TextRanges[i].TextRun;
                    Dispatcher.Invoke(() =>
                    {
                        mainSbar.Value++;
                        if (_hexText)
                            txt.Text = textProccess(txt.Text).Replace("0A", "\r");
                        else
                            txt.Text = textProccess(txt.Text.Replace("\r", "0A"));
                    });
                }
                HideSbar();
            });
        }

        private void HideSbar()
        {
            Dispatcher.Invoke(() =>
            {
                mainSbar.Visibility = Visibility.Hidden;
            });
        }

        private void SetSBar(int max, int min = 0, int val = 0)
        {
            Dispatcher.Invoke(() =>
            {
                mainSbar.Visibility = Visibility.Visible;
                mainSbar.Minimum = min;
                mainSbar.Maximum = max;
                mainSbar.Value = val;
            });
        }

        private void SendData()
        {
            if (_serialPort.IsOpen && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                _serialPort.WriteLine(InputTextBox.Text);
                AppendLineToRichTextBox($"Sent: {InputTextBox.Text.Replace("\n", "\n    ")}\n", _sent);
                InputTextBox.Clear();
            }
        }

        private void SendData(string text)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.WriteLine(text);
                AppendLineToRichTextBox($"Sent: {text.Replace("\n", "\n    ")}\n", _sent);
            }
        }


        private void AppendLineToRichTextBox(string text, string colorResourceKey = "", string dateTime = "")
        {
            if (_hexText)
                text = text.ToHex();/*.Replace("0A", "\r");*/

            if (dateTime == "")
                dateTime = DateTime.Now.ToString("HH:mm:ss:ffff");



            var start = DataTextBox.Document.ContentEnd;
            Run run = new Run(text, DataTextBox.Document.ContentEnd);
            Run timeInline = new Run(dateTime);
            var runTime = new Span(timeInline, DataTextBox.Document.ContentEnd);

            //Paragraph paragraph = new Paragraph();
            //paragraph.Inlines.Add(run);

            //var run = new Run(text, DataTextBox.Document.ContentEnd);
            // Convert the Color to a SolidColorBrush
            //SolidColorBrush brush = new SolidColorBrush((Color)color);
            //textRange.ApplyPropertyValue(TextElement.ForegroundProperty, color);


            if (string.IsNullOrEmpty(colorResourceKey))
                colorResourceKey = _default;

            runTime.Style = (Style)FindResource(_TimeResourceKey);

            runTime.Inlines.Add(new Run(" : "));
            var span = new Span(runTime, start);
            span.Inlines.Add(run);
            span.Inlines.Add(new Run("\r"));
            span.Style = (Style)FindResource(colorResourceKey);

            //DataTextBox.Document.Blocks.Add(paragraph);
            mainParagraph.Inlines.Add(span);

            _textRanges.Add(new TextboxItem { TextRun = run, TimeRun = timeInline, ResourceKey = colorResourceKey });

            if (_scrollToEnd)
                DataTextBox.ScrollToEnd();
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+"); // Regex that matches non-numeric text
            return !regex.IsMatch(text);
        }
        #endregion

        #region Click Events
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                PortClose();
            }
            else
            {
                PortOpen();
            }
        }

        private void PortOpen()
        {
            if (PortComboBox.SelectedItem == null)
            {
                AppendLineToRichTextBox("Select the Port", _fail);
                return;
            }
            // config
            _serialPort.PortName = PortComboBox.SelectedItem.ToString();
            _serialPort.BaudRate = (int)BaudRateComboBox.SelectedItem;
            _serialPort.Parity = GetSelectedParity();
            _serialPort.StopBits = GetSelectedStopBits();
            //_serialPort.Handshake = Handshake.
            try
            {
                _serialPort.Open();
                ConnectButton.Content = _disconnectBtnText;
                AppendLineToRichTextBox("Connected.\n", _success);
            }
            catch (Exception ex)
            {
                ConnectButton.Content = _connectBtnText;
                AppendLineToRichTextBox(ex.Message, _fail);
            }
        }

        private void PortClose()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
            Dispatcher.Invoke(() =>
            {
                ConnectButton.Content = _connectBtnText;
                AppendLineToRichTextBox("Disconnected.\n", _fail);
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

        private void Clear_button_Click(object sender, RoutedEventArgs e)
        {
            mainParagraph.Inlines.Clear();
            TextRanges.Clear();
        }

        private void ClearHex_button_Click(object sender, RoutedEventArgs e)
        {
            HexDataTextBox.Document.Blocks.Clear();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendData();
        }

        private void SendSavedCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SendListViewItem item)
            {
                //MessageBox.Show($"Send button clicked for {item.Text} with delay {item.Delay}");

                SendData(item.Text);
            }
        }

        private async void AutoRunBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_autoRunTask != null && !_autoRunTask.IsCompleted)
            {
                // If the task is already running, cancel it
                _cancellationTokenSource.Cancel();
                AutoRunBtn.IsEnabled = false; // Disable button to prevent multiple clicks during cancellation
                return;
            }

            // Initialize the CancellationTokenSource
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            AutoRunBtn.Content = "Cancel"; // Change button text to "Cancel"

            // Create and start the task
            _autoRunTask = Task.Run(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() => RemoveBtn.IsEnabled = false);
                    foreach (var item in SendItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation

                        if (!item.CanSend) continue;

                        if (item.Delay > 0)
                            await Task.Delay(item.Delay, cancellationToken); // Pass the cancellation token

                        Dispatcher.Invoke(() => SendData(item.Text));
                        Dispatcher.Invoke(() => ListView.SelectedItem = item);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle the cancellation
                }
                finally
                {
                    Dispatcher.Invoke(() => RemoveBtn.IsEnabled = true);
                    // Revert the button text and state after completion or cancellation
                    Dispatcher.Invoke(() =>
                    {
                        AutoRunBtn.Content = "Auto Send";
                        AutoRunBtn.IsEnabled = true;
                    });
                }
            }, cancellationToken);

            await _autoRunTask;
        }

        #endregion

        #region Events 
        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort.IsOpen && _serialPort.PortName != null)
            {
                try
                {
                    string data = _serialPort.ReadLine();
                    await Task.Run(() =>
                    {
                        Dispatcher.Invoke(() => AppendLineToRichTextBox(data));
                    });
                }
                catch (Exception ex)
                {
                    PortClose();
                    AppendLineToRichTextBox(ex.Message, _fail);
                }
            }
            else
            {
                PortClose();
            }
        }

        private void AddNewSendItem_Click(object sender, RoutedEventArgs e)
        {
            //AddItemWindow addItemWindow = new AddItemWindow();
            //if (addItemWindow.ShowDialog() == true)
            //{
            //    SendItems.Add(addItemWindow.NewItem);
            //}
            if (_autoRunTask == null || _autoRunTask.IsCompleted)
                SendItems.Add(new SendListViewItem());
            else
                MessageBox.Show("AutoRun is not Completed !", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItems.Count == 0)
                return;

            var selectedIndex = ListView.SelectedIndex;

            SendItems.RemoveAt(selectedIndex);
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

        private void Scroll_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _scrollToEnd = (bool)Scroll_checkbox.IsChecked;
            //Scroll_checkbox.IsChecked = _scrollToEnd;

            if (_scrollToEnd)
                DataTextBox.ScrollToEnd();
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

        private void CheckPortChanges(object? sender, ElapsedEventArgs e)
        {
            string[] currentPorts = SerialPort.GetPortNames();
            if (!EqualArrays(_lastKnownPorts, currentPorts))
            {
                Dispatcher.Invoke(() =>
                {
                    if (PortComboBox.SelectedItem == null ||
                        PortComboBox.SelectedItem.ToString() != _serialPort.PortName ||
                        currentPorts.Length <= 0)
                        PortClose();
                });

                _lastKnownPorts = currentPorts;
                Dispatcher.Invoke(() => PortComboBox.ItemsSource = currentPorts);
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }
        #endregion

        #region Saving and Loading
        public void SavePage()
        {
            saveConfig();
            saveAutoSendItems();
            saveText();
        }

        public void LoadPage()
        {
            loadConfig();
            loadAutoSendItems();
            loadText();
        }

        private void saveText()
        {
            PageStorage.Save(PathExtentions.LogsPath, _textRanges.Select(x => new TextBoxInlineItem
            {
                Text = x.TextRun.Text,
                Time = x.TimeRun.Text,
                ResourceKey = x.ResourceKey
            }));
        }
        private async void loadText()
        {
            await Task.Run(() =>
            {
                _textRanges.Clear();
                Dispatcher.Invoke(() => { mainParagraph.Inlines.Clear(); });

                var loadedText = PageStorage.Load<List<TextBoxInlineItem>>(PathExtentions.LogsPath);

                foreach (var item in loadedText)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendLineToRichTextBox(item.Text, item.ResourceKey, item.Time);
                    });
                }

            });
        }
        private void saveConfig()
        {
            var config = new SerialConfig()
            {
                PortName = PortComboBox.SelectedItem?.ToString(),
                BaudRate = (int)BaudRateComboBox.SelectedItem,
                Parity = GetSelectedParity(),
                StopBits = GetSelectedStopBits(),
            };
            PageStorage.Save(PathExtentions.SerialConfigPath, config);
        }
        private void loadConfig()
        {
            var config = PageStorage.Load<SerialConfig>(PathExtentions.SerialConfigPath);
            PortComboBox.SelectedValue = config.PortName;
            BaudRateComboBox.SelectedValue = config.BaudRate;
            ParityComboBox.SelectedIndex = (int)config.Parity;
            StopBitComboBox.SelectedIndex = (int)config.StopBits;
        }
        private void loadAutoSendItems()
        {
            this.SendItems.Clear();
            var items = PageStorage.Load<ObservableCollection<SendListViewItem>>(PathExtentions.SendSerialItemsPath);
            this.SendItems = items;
        }
        private void saveAutoSendItems()
        {
            PageStorage.Save(PathExtentions.SendSerialItemsPath, this.SendItems);
        }
        #endregion

    }
}
