// Views/RegisterPage.xaml.cs
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BearingProductionApp.Helpers;
using WpfApp1;

namespace BearingProductionApp.Views
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void LoadRoles()
        {
            try
            {
                RoleComboBox.ItemsSource = AppConnect.Model.Roles.ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                RoleComboBox.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Используем правильное имя поля: login (с маленькой буквы)
                if (AppConnect.Model.Users.Any(u => u.login == LoginTextBox.Text))
                {
                    MessageBox.Show("Логин уже занят!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var user = new Users
                {
                    Name = NameTextBox.Text,
                    login = LoginTextBox.Text,        // маленькая буква
                    Password = PasswordBox.Password,
                    IdRole = (RoleComboBox.SelectedItem as Roles).id  // маленькая буква
                };

                AppConnect.Model.Users.Add(user);
                AppConnect.Model.SaveChanges();

                MessageBox.Show("Пользователь зарегистрирован!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AppFrame.MainFrame.Navigate(new LoginPage());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.MainFrame.Navigate(new LoginPage());
        }
    }
}