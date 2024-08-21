using SerialM.Business.Exceptions;
using SerialM.Business.Utilities;
using SerialM.Business.Utilities.Extensions;
using SerialM.Endpoint.WPF.Data;
using SerialM.Endpoint.WPF.Interfaces;
using SerialM.Endpoint.WPF.Models;
using SerialM.Endpoint.WPF.Validation;
using SerialM.Endpoint.WPF.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace SerialM.Endpoint.WPF.Pages
{
    /// <summary>
    /// Interaction logic for SerialTerminal.xaml
    /// </summary>
    public partial class SerialTerminal : Page, ISaveablePage, IDataPersistence
    {

        #region Data
        private const string _disconnectBtnText = "Disconnect";
        private const string _connectBtnText = "Connect";
        private static SerialPort _serialPort;
        private System.Timers.Timer _portCheckTimer;
        private string[] _lastKnownPorts;

        TerminalPageData _pageData;
        #endregion

        public SerialTerminal()
        {
            InitializeComponent();
            _pageData = new(this, DataTextBox, ListView, mainSbar);

            InitializeSerialPort();
            LoadAvailablePorts();
            LoadPortSetting();
            InitializePortCheckTimer();
            Scroll_checkbox.IsChecked = true;
            _pageData.Initialize();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //SavePage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadPage();
        }



        #region Methods
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
                _pageData.AppendLineToRichTextBox($"{InputTextBox.Text.Replace("\n", "\n    ")}\n", TerminalPageData.SENT_RESOURCEKEY);
                InputTextBox.Clear();
            }
        }

        private void SendData(string text)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.WriteLine(text);
                _pageData.AppendLineToRichTextBox($"Sent: {text.Replace("\n", "\n    ")}\n", TerminalPageData.SENT_RESOURCEKEY);
            }
        }

        private void OnEndOrCancelAutoSend()
        {
            // Revert the button text and state after completion or cancellation
            Dispatcher.Invoke(() =>
            {
                RemoveBtn.IsEnabled = true;
                AutoRunBtn.Content = "Auto Send";
                AutoRunBtn.IsEnabled = true;
            });
        }

        private void OnStartAutoSend()
        {
            Dispatcher.Invoke(() =>
            {
                AutoRunBtn.Content = "Cancel"; // Change button text to "Cancel"
                RemoveBtn.IsEnabled = false;
            });
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
                _pageData.AppendLineToRichTextBox("Select the Port", TerminalPageData.FAIL_RESOURCEKEY);
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
                _pageData.AppendLineToRichTextBox("Connected.\n", TerminalPageData.SUCCESS_RESOURCEKEY);
            }
            catch (Exception ex)
            {
                ConnectButton.Content = _connectBtnText;
                _pageData.AppendLineToRichTextBox(ex.Message, TerminalPageData.FAIL_RESOURCEKEY);
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
                _pageData.AppendLineToRichTextBox("Disconnected.\n", TerminalPageData.FAIL_RESOURCEKEY);
            });
        }

        private void ToHex_button_Click(object sender, RoutedEventArgs e)
        {
            var selected = DataTextBox.Selection.Text;
            var hexSelected = "";
            if (!_pageData.HexText)
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
            _pageData.ClearTexts();
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
            if (_pageData.IsAutoSendRunning)
            {
                // If the task is already running, cancel it
                _pageData.CancelAutoSendItems(OnEndOrCancelAutoSend);
                return;
            }

            await _pageData.StartAutoSendItems(SendData, OnStartAutoSend, OnEndOrCancelAutoSend);
        }

        private void AddCopySendItem_Click(object sender, RoutedEventArgs e)
        {
            _pageData.AddCopySendItem(_pageData.SendListView.SelectedIndex);
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
                        Dispatcher.Invoke(() => _pageData.AppendLineToRichTextBox(data));
                    });
                }
                catch (Exception ex)
                {
                    PortClose();
                    _pageData.AppendLineToRichTextBox(ex.Message, TerminalPageData.FAIL_RESOURCEKEY);
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
            try
            {
                _pageData.AddNewSendItem();
            }
            catch (NotCompletedTaskException ex)
            {
                MessageBox.Show("Auto Send is not Completed !", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItems.Count == 0)
                return;

            var selectedIndex = ListView.SelectedIndex;

            _pageData.SendItems.RemoveAt(selectedIndex);
        }

        private void HEX_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _pageData.HexText = (bool)HEX_checkbox.IsChecked;

            _pageData.TaskTextProccess((s) =>
            {
                if (_pageData.HexText)
                    return s.ToHex().Replace("0A", "\r");
                else
                    return s.Replace("\r", "0A").FromHex();
            });

        }

        private void Scroll_checkbox_Checked(object sender, RoutedEventArgs e)
        {
            _pageData.ScrollToEnd = (bool)Scroll_checkbox.IsChecked;
            //Scroll_checkbox.IsChecked = _scrollToEnd;

            if (_pageData.ScrollToEnd)
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
                Dispatcher.Invoke(() =>
                {
                    PortComboBox.ItemsSource = currentPorts;
                    if (PortComboBox.Items.Count > 0)
                        PortComboBox.SelectedIndex = 0;
                });
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsNumeric();
        }
        #endregion
        private void LogSplitter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //var size = LogSplitter.ActualWidth;
            //InputTextBox.Text = size.ToString();
        }

        #region Saving and Loading
        public void SavePage()
        {
            SaveConfig();
            SaveAutoSendItems();
            SaveText();
        }

        public void LoadPage()
        {
            LoadConfig();
            LoadAutoSendItems();
            LoadText();
        }


        public void SaveText()
        {
            PageStorage.Save(PathExtentions.SerialLogsPath, _pageData.TextRanges.Select(x => new TextBoxInlineItem
            {
                Text = x.TextRun.Text,
                Time = x.TimeRun.Text,
                ResourceKey = x.ResourceKey
            }));
        }
        public async void LoadText()
        {
            //var loading = new LoadingWindow();
            //loading.Show();
            //loading.Focus();
            await Task.Run(() =>
            {
                _pageData.ClearTexts();

                var loadedText = PageStorage.Load<List<TextBoxInlineItem>>(PathExtentions.SerialLogsPath);
                _pageData.SetSBar(loadedText.Count, 0, 0);

                foreach (var item in loadedText)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _pageData.SbarValue++;
                        _pageData.AppendLineToRichTextBox(item.Text, item.ResourceKey, item.Time);
                    });
                }
                _pageData.HideSbar();
            });
            //loading.Close();
        }
        public void SaveConfig()
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
        public void LoadConfig()
        {
            var config = PageStorage.Load<SerialConfig>(PathExtentions.SerialConfigPath);
            PortComboBox.SelectedValue = config.PortName;
            BaudRateComboBox.SelectedValue = config.BaudRate;
            ParityComboBox.SelectedIndex = (int)config.Parity;
            StopBitComboBox.SelectedIndex = (int)config.StopBits;
        }
        public void LoadAutoSendItems()
        {
            _pageData.SendItems.Clear();
            var items = PageStorage.Load<ObservableCollection<SendListViewItem>>(PathExtentions.SendSerialItemsPath);
            _pageData.SendItems = items;
        }
        public void SaveAutoSendItems()
        {
            PageStorage.Save(PathExtentions.SendSerialItemsPath, _pageData.SendItems);
        }
        #endregion

    }
}
