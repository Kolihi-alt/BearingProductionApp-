using System.Windows;
using BearingProductionApp.Helpers;
using BearingProductionApp.Views;

namespace BearingProductionApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppFrame.MainFrame = MainFrame;
            AppFrame.MainFrame.Navigate(new LoginPage());
        }
    }
}