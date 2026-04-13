// Views/LoginPage.xaml.cs
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BearingProductionApp.Helpers;
using BearingProductionApp.Views;

namespace BearingProductionApp.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Используем правильные имена полей из БД: login и Password
                var user = AppConnect.Model.Users.FirstOrDefault(u =>
                    u.login == LoginTextBox.Text && u.Password == PasswordBox.Password);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                switch (user.IdRole)
                {
                    case 1: // Мастер
                        AppFrame.MainFrame.Navigate(new MasterPage());
                        break;
                    case 2: // Рабочий
                        AppFrame.MainFrame.Navigate(new WorkerPage(user.id));
                        break;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.MainFrame.Navigate(new RegisterPage());
        }
    }
}