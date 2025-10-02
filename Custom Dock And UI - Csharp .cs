using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace VelocityDock
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LaunchApp(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button.Content.ToString() == @"C:\Nodes\Documents")
            {
                Process.Start("explorer.exe", @"C:\Nodes\Documents");
            }
            else if (button.Content.ToString() == "Installer")
            {
                Process.Start("dotnet", @"run --project C:\VelocityObjects\Installer\VelocityInstaller");
            }
            else
            {
                Process.Start(button.Content.ToString());
            }
        }
    }
}