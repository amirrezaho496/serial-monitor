using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {

        public double SbarValue
        {
            get => mainSbar.Value;
            set => SetSbarValue(value);
        }

        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void SetSBar(int max, int min = 0, int initValue = 0, string label = "loading ...")
        {
            Dispatcher.Invoke(() =>
            {
                mainSbar.Minimum = min;
                mainSbar.Maximum = max;
                mainSbar.Value = initValue;
                SbarLabel.Content = label;
            });
        }

        public void SetSbarValue(double val)
        {
            Dispatcher.Invoke(() =>
            {
                mainSbar.Value = val;
            });
        }

        public void SetLabel(string label)
        {
            SbarLabel.Content = label;
        }

        public ProgressBar ProgressBar => mainSbar;
    }
}
