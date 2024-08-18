using SerialM.Business.Exceptions;
using SerialM.Business.Network;
using SerialM.Business.Utilities;
using SerialM.Business.Utilities.Extensions;
using SerialM.Endpoint.WPF.Data;
using SerialM.Endpoint.WPF.Data.Models;
using SerialM.Endpoint.WPF.Interfaces;
using SerialM.Endpoint.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class NetworkTerminal : Page,ISaveablePage, IDataPersistence
    {
        #region Data
        private System.Timers.Timer _portCheckTimer;
        TerminalPageData _pageData;
        #endregion

        #region Property
        private string InputIp
        {
            get => InputIpTextBox.Text;
            set
            {
                if (InputIpTextBox.Text.IsIPAddress(out _))
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
                if (InputIpTextBox.Text.IsValidPort(out _))
                {
                    InputIpTextBox.Text = value.ToString();
                }
                else
                    throw new FormatException("The input is not a valid Port Number.");
            }
        }
        #endregion

        public NetworkTerminal()
        {
            InitializeComponent();

            _pageData = new(this, DataTextBox, ListView, mainSbar);
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
            if (ListView.SelectedItems.Count == 0)
                return;

            var selectedIndex = ListView.SelectedIndex;

            _pageData.SendItems.RemoveAt(selectedIndex);
        }

        private void SendSavedCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SendListViewItem item)
            {
                //MessageBox.Show($"Send button clicked for {item.Text} with delay {item.Delay}");

                SendData(item.Text);
            }
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
                    return s.FromHex().Replace("\r", "0A");
            });
        }

        private void Scroll_checkbox_Checked(object sender, RoutedEventArgs e)
        {
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
        }

        private void SendData(string text)
        {
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
            await Task.Run(() =>
            {
                _pageData.ClearTexts();

                var loadedText = PageStorage.Load<List<TextBoxInlineItem>>(PathExtentions.NetworkLogsPath);
                _pageData.SetSBar(loadedText.Count, 0, 0);

                foreach (var item in loadedText)
                {
                    Dispatcher.Invoke(() =>
                    {
                        mainSbar.Value += 1;
                        _pageData.AppendLineToRichTextBox(item.Text, item.ResourceKey, item.Time);
                    });
                }
                _pageData.HideSbar();
            });
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
