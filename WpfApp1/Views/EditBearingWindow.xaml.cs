using System;
using System.Windows;
using BearingProductionApp.ViewModels;
using WpfApp1;

namespace BearingProductionApp.Views
{
    public partial class EditBearingWindow : Window
    {
        private readonly MasterViewModel _viewModel;
        private readonly BearingTypes _bearingToEdit;

        public EditBearingWindow(MasterViewModel viewModel, BearingTypes bearing)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _bearingToEdit = bearing;

            // Заполняем поля текущими значениями
            EditTypeNameTextBox.Text = _bearingToEdit.TypeName;
            EditTypePriceTextBox.Text = _bearingToEdit.Price.ToString("F2");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditTypeNameTextBox.Text) || string.IsNullOrWhiteSpace(EditTypePriceTextBox.Text))
            {
                MessageBox.Show("Введите название и цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal newPrice;
            if (!decimal.TryParse(EditTypePriceTextBox.Text, out newPrice) || newPrice <= 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _bearingToEdit.TypeName = EditTypeNameTextBox.Text;
            _bearingToEdit.Price = newPrice;
            _viewModel.UpdateBearingType(); // Вызываем метод обновления
            DialogResult = true; // Закрываем окно с успехом
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Закрываем окно без сохранения
        }
    }
}