using SerialMonitor;
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
using System.Windows.Shapes;

namespace SerialM.Endpoint.WPF.Windows
{
    /// <summary>
    /// Interaction logic for ExternalPage.xaml
    /// </summary>
    public partial class ExternalPage : Window
    {
        private Page _exPage;
        private MainWindow _mainWindow;
        public ExternalPage(Page page, MainWindow mainWindow)
        {
            InitializeComponent();
            ExPage = page;
            _mainWindow = mainWindow;
            PageFrame.Unloaded += (s,e) => Close();
        }

        public Page ExPage
        {
            get => _exPage;
            set
            {
                if (_exPage != value)
                {
                    _exPage = value;
                    PageFrame.Content = _exPage;
                    Title = _exPage.Title;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mainWindow.DeleteWindow(this);
        }
    }
}
