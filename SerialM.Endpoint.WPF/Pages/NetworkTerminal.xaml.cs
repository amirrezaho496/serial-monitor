using SerialM.Business.Exceptions;
using SerialM.Business.Network;
using SerialM.Business.Network.Interfaces;
using SerialM.Business.Utilities;
using SerialM.Business.Utilities.Extensions;
using SerialM.Endpoint.WPF.Data;
using SerialM.Endpoint.WPF.Data.Models;
using SerialM.Endpoint.WPF.Interfaces;
using SerialM.Endpoint.WPF.Models;
using SerialM.Endpoint.WPF.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class NetworkTerminal : Page, ISaveablePage, IDataPersistence, INotifyPropertyChanged
    {
        #region Data
        private System.Timers.Timer _portCheckTimer;
        TerminalPageData _pageData;
        private INetworkDevice _networkDevice;
        private bool _isConnected = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Property
        private string InputIp
        {
            get => InputIpTextBox.Text;
            set
            {
                if (value.IsIPAddress(out _))
                {
                    InputIpTextBox.Text = value;
                }
                else
                    throw new FormatException("The input is not a valid IP address.");
            }
        }


        private NetworkMode InputNetworkMode
        {
            get => (NetworkMode)Enum.Parse(typeof(NetworkMode), ModeComboBox.SelectedItem.ToString());
            set => ModeComboBox.SelectedIndex = (int)value;
        }

        private int InputPort
        {
            get => int.Parse(PortTextBox.Text);
            set
            {
                if (value.ToString().IsValidPort(out _))
                {
                    PortTextBox.Text = value.ToString();
                }
                else
                    throw new FormatException("The input is not a valid Port Number.");
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public NetworkTerminal()
        {
            InitializeComponent();

            _pageData = new(this, DataTextBox, ListView, mainSbar);
            LoadNModeInputs();
            Scroll_checkbox.IsChecked = true;
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
            if (_pageData.IsAutoSendRunning)
                throw new NotCompletedTaskException("Can not add new item when Auto send is running...");

            _pageData.AddNewSendItem();
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

        private void ClearHex_button_Click(object sender, RoutedEventArgs e)
        {
            HexDataTextBox.Document.Blocks.Clear();
        }
        private void Clear_button_Click(object sender, RoutedEventArgs e)
        {
            _pageData.ClearTexts();
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            _pageData.RemoveSelectedItems();
        }

        private void SendSavedCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SendListViewItem item)
            {
                //MessageBox.Show($"Send button clicked for {item.Text} with delay {item.Delay}");

                SendData(item.Text);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            IsConnected = !IsConnected;

            if (!IsConnected)
            {
                Disconnect();
                return;
            }

            if (InputNetworkMode == NetworkMode.Server)
            {
                var server = new NetworkServer();
                server.OnServerStarted += OnNetworkStarted;
                server.OnMessageReceived += OnMessageRecieved;
                server.OnClientConnected += (msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _pageData.AppendLineToRichTextBox(msg, TerminalPageData.SUCCESS_RESOURCEKEY);
                    });
                };

                server.OnClientDisconnected += OnNetworkDisconnected;
                server.OnStartError += OnNetworkStartError;

                _networkDevice = server;
            }
            else
            {
                var client = new NetworkClient();
                client.OnClientConnected += OnNetworkStarted;
                client.OnMessageReceived += OnMessageRecieved;
                client.OnClientDisconnected += OnNetworkDisconnected;
                client.OnStartError += OnNetworkStartError;

                _networkDevice = client;
            }

            _networkDevice.StartAsync(InputIp, InputPort);
            ConnectButton.Content = "Disconnect";
        }
        private void AddCopySendItem_Click(object sender, RoutedEventArgs e)
        {
            _pageData.AddCopyOfSelectedItems();
        }
        #endregion

        #region Preview Events
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.IsNumeric();
        }
        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                SendData();
                e.Handled = true;
            }
        }

        #endregion

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                SendData();
            }
        }

        #region Change Events

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
            if (_pageData == null)
                return;

            _pageData.ScrollToEnd = (bool)Scroll_checkbox.IsChecked;
            //Scroll_checkbox.IsChecked = _scrollToEnd;

            if (_pageData.ScrollToEnd)
                DataTextBox.ScrollToEnd();
        }
        private void LogSplitter_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
        #endregion

        #region Methods

        private void LoadNModeInputs()
        {
            ModeComboBox.ItemsSource = Enum.GetNames(typeof(NetworkMode));
            ModeComboBox.SelectedIndex = 0;
        }

        private void SendData()
        {
            if (_networkDevice == null) return;
            if (_networkDevice.Connected && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                _networkDevice.SendMessageAsync(InputTextBox.Text + '\n');
                _pageData.AppendLineToRichTextBox($"{InputTextBox.Text.Replace("\n", "\n    ")}\n", TerminalPageData.SENT_RESOURCEKEY);
                InputTextBox.Clear();
            }
        }

        private void SendData(string text)
        {
            if (_networkDevice == null)
                return;

            _networkDevice.SendMessageAsync(text + '\n');
            string msg = $"Sent : {text}";
            _pageData.AppendLineToRichTextBox(msg, TerminalPageData.SENT_RESOURCEKEY);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnNetworkStarted(string msg)
        {
            _pageData.AppendLineToRichTextBox(msg, TerminalPageData.SUCCESS_RESOURCEKEY);
        }

        private void OnMessageRecieved(string msg)
        {
            _pageData.AppendLineToRichTextBox(msg);
        }

        private void OnNetworkDisconnected(string msg)
        {
            _pageData.AppendLineToRichTextBox(msg, TerminalPageData.FAIL_RESOURCEKEY);
            _networkDevice.Dispose();
            Disconnect();
        }
        private void OnNetworkStartError(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                _pageData.AppendLineToRichTextBox(msg, TerminalPageData.FAIL_RESOURCEKEY);
            });
            Disconnect();
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

        private void Disconnect()
        {
            IsConnected = false;
            _networkDevice.Dispose();
            Dispatcher.Invoke(() =>
            {
                ConnectButton.Content = "Connect";
            });
        }
        #endregion



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
            PageStorage.Save(PathExtentions.NetworkLogsPath, _pageData.TextRanges.Select(x => new TextBoxInlineItem
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

                var loadedText = PageStorage.Load<List<TextBoxInlineItem>>(PathExtentions.NetworkLogsPath);
                _pageData.SetSBar(loadedText.Count, 0, 0);

                foreach (var item in loadedText)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _pageData.SbarValue++;
                    });
                    _pageData.AppendLineToRichTextBox(item.Text, item.ResourceKey, item.Time);
                }
                _pageData.HideSbar();
            });
            //loading.Close();
        }
        public void SaveConfig()
        {
            //store config in 'config'
            var config = new NetworkConfig()
            {
                IP = InputIp,
                Port = InputPort,
                NetworkMode = InputNetworkMode
            };
            PageStorage.Save(PathExtentions.NetworkConfigPath, config);
        }
        public void LoadConfig()
        {
            var config = PageStorage.Load<NetworkConfig>(PathExtentions.NetworkConfigPath);
            InputIp = config.IP;
            InputPort = config.Port;
            InputNetworkMode = config.NetworkMode;
        }
        public void LoadAutoSendItems()
        {
            _pageData.SendItems.Clear();
            var items = PageStorage.Load<ObservableCollection<SendListViewItem>>(PathExtentions.SendNetworkItemsPath);
            _pageData.SendItems = items;
        }
        public void SaveAutoSendItems()
        {
            PageStorage.Save(PathExtentions.SendNetworkItemsPath, _pageData.SendItems);
        }
        #endregion
    }
}
