using InCenterless.Views._1.Home;
using InCenterless.Views._2.Mode;
using InCenterless.Views._3.Setting;
using InCenterless.Views._4.Maintenance;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace InCenterless.Controls
{
    /// <summary>
    /// Interaction logic for TopBar.xaml
    /// </summary>
    public partial class TopBar : UserControl
    {
        public TopBar()
        {
            InitializeComponent();
            UpdateTime();

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) => UpdateTime();
            timer.Start();
        }


        private void UpdateTime()
        {
            TimeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
            DateTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void Navigate(Page page)
        {
            var navService = NavigationService.GetNavigationService(this);
            navService?.Navigate(page);
        }


        private void MinimizeButton(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;

        }

        private void HomeButton(object sender, RoutedEventArgs e)
        {
            Navigate(new MachineConditionPage());
        }

        private void ModeButton(object sender, RoutedEventArgs e)
        {
            Navigate(new CycleMonitorPage());
        }

        private void SetButton(object sender, RoutedEventArgs e)
        {
            Navigate(new CycleDataPage());
        }

        private void MaintButton(object sender, RoutedEventArgs e)
        {
            Navigate(new ReferencePage());
        }
    }
}
