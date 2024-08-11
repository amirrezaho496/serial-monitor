using Microsoft.Win32;
using ModernWpf;
using SerialM.Business.Utilities;
using SerialM.Endpoint.WPF.Controls;
using SerialM.Endpoint.WPF.Interfaces;
using SerialM.Endpoint.WPF.Models;
using SerialM.Endpoint.WPF.Pages;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SerialMonitor
{
    public partial class MainWindow : Window, ISaveable
    {
        static SerialTerminal SerialTerminal = new SerialTerminal();
        
        static List<Page> pages = new List<Page>();

        public string Path { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            pages.Add(SerialTerminal);
            pages.Add(new Page2());
        }


        #region Menu
        // Menu item event handlers
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                //DataTextBox.AppendText($"Opened file: {openFileDialog.FileName}\n");
                //TODO: Load file content if needed
                Path = openFileDialog.FileName;
                Load();
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                //AppendTextToRichTextBox($"Saved file: {saveFileDialog.FileName}\n", _info);
                // TODO: Save file content if needed
                Path = saveFileDialog.FileName;
                Save();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //if (!string.IsNullOrEmpty(DataTextBox.Selection.Text))
            //{
            //    Clipboard.SetText(DataTextBox.Selection.Text);
            //}
        }

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //if (Clipboard.ContainsText())
            //{
            //    InputTextBox.Text = Clipboard.GetText();
            //}
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Serial Monitor v1.0\nAuthor: AMirreza", "About");
        }
        #endregion

        private void sidebar_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = sidebar.SelectedIndex;
            NavFrame.Navigate(MainWindow.pages[selected % MainWindow.pages.Count]);
        }
        private void LightThemeMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearValue(ThemeManager.RequestedThemeProperty);

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var tm = ThemeManager.Current;
                if (tm.ActualApplicationTheme == ApplicationTheme.Dark)
                {
                    tm.ApplicationTheme = ApplicationTheme.Light;
                    LightThemeMenuBtn.IsChecked = true;
                    DarkThemeMenuBtn.IsChecked = false;
                }
            });
        }

        private void DarkThemeMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearValue(ThemeManager.RequestedThemeProperty);

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var tm = ThemeManager.Current;
                if (tm.ActualApplicationTheme == ApplicationTheme.Light)
                {
                    tm.ApplicationTheme = ApplicationTheme.Dark;
                    LightThemeMenuBtn.IsChecked = false;
                    DarkThemeMenuBtn.IsChecked = true;
                }
            });
        }

        public void Save()
        {
        }

        public void Load()
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SerialTerminal.SavePage();
        }
    }
}
