// Views/WorkerPage.xaml.cs (без дублирующего класса)
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BearingProductionApp.Helpers;
using Excel = Microsoft.Office.Interop.Excel;
using WpfApp1;

namespace BearingProductionApp.Views
{
    public partial class WorkerPage : Page
    {
        private readonly int _userId;
        private string _workerName;
        private ObservableCollection<ProductionRecords> _allMyRecords;
        private ListCollectionView _recordsView;

        public WorkerPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var user = context.Users.Find(_userId);
                    _workerName = user?.Name ?? "Рабочий";

                    var bearingTypes = context.BearingTypes.Where(b => b.IsActive == true).ToList();
                    BearingTypeCombo.ItemsSource = bearingTypes;

                    _allMyRecords = new ObservableCollection<ProductionRecords>(
                        context.ProductionRecords
                            .Include("BearingTypes")
                            .Where(r => r.UserId == _userId)
                            .OrderByDescending(r => r.ProductionDate)
                            .ToList());

                    MyRecordsListView.ItemsSource = _allMyRecords;
                    _recordsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_allMyRecords);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (BearingTypeCombo.SelectedValue == null || string.IsNullOrWhiteSpace(QuantityBox.Text))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    string status = quantity >= 50 ? "Отлично" : (quantity >= 30 ? "Хорошо" : "Норма");

                    var record = new ProductionRecords
                    {
                        UserId = _userId,
                        BearingTypeId = (int)BearingTypeCombo.SelectedValue,
                        Quantity = quantity,
                        ProductionDate = DateTime.Now,
                        Status = status
                    };

                    context.ProductionRecords.Add(record);
                    context.SaveChanges();

                    record.BearingTypes = context.BearingTypes.Find(record.BearingTypeId);
                    _allMyRecords.Insert(0, record);

                    QuantityBox.Text = "";
                    MessageBox.Show("Запись добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            BearingTypeCombo.SelectedIndex = -1;
            QuantityBox.Text = "";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            DateFrom.SelectedDate = null;
            DateTo.SelectedDate = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_recordsView == null) return;

            _recordsView.Filter = item =>
            {
                var record = item as ProductionRecords;
                if (record == null) return false;

                if (DateFrom.SelectedDate.HasValue && record.ProductionDate < DateFrom.SelectedDate.Value)
                    return false;
                if (DateTo.SelectedDate.HasValue && record.ProductionDate > DateTo.SelectedDate.Value.AddDays(1))
                    return false;

                string searchText = SearchBox.Text.ToLower();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    return (record.BearingTypes?.TypeName?.ToLower().Contains(searchText) ?? false) ||
                           record.Quantity.ToString().Contains(searchText) ||
                           (record.Status?.ToLower().Contains(searchText) ?? false);
                }

                return true;
            };
        }

        private void GroupCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_recordsView == null) return;

            _recordsView.GroupDescriptions.Clear();

            var selected = (GroupCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (selected)
            {
                case "По дате":
                    _recordsView.GroupDescriptions.Add(new PropertyGroupDescription("ProductionDate"));
                    break;
                case "По статусу":
                    _recordsView.GroupDescriptions.Add(new PropertyGroupDescription("Status"));
                    break;
                case "По типу":
                    _recordsView.GroupDescriptions.Add(new PropertyGroupDescription("BearingTypes.TypeName"));
                    break;
            }

            _recordsView.Refresh();
        }

        private void EditMyRecord_Click(object sender, RoutedEventArgs e)
        {
            var selected = MyRecordsListView.SelectedItem as ProductionRecords;
            if (selected == null)
            {
                MessageBox.Show("Выберите запись!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Простой диалог редактирования
            var dialog = new Window
            {
                Title = "Редактирование записи",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            stackPanel.Children.Add(new TextBlock { Text = "Количество:", FontWeight = FontWeights.Bold });
            var quantityBox = new TextBox { Text = selected.Quantity.ToString(), Margin = new Thickness(0, 5, 0, 10), Height = 25 };
            stackPanel.Children.Add(quantityBox);

            stackPanel.Children.Add(new TextBlock { Text = "Статус:", FontWeight = FontWeights.Bold });
            var statusCombo = new ComboBox { Margin = new Thickness(0, 5, 0, 15), Height = 25 };
            statusCombo.Items.Add("Отлично");
            statusCombo.Items.Add("Хорошо");
            statusCombo.Items.Add("Норма");
            statusCombo.SelectedItem = selected.Status;
            stackPanel.Children.Add(statusCombo);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5), Height = 28 };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5), Height = 28 };

            okButton.Click += (s, args) =>
            {
                if (int.TryParse(quantityBox.Text, out int qty) && qty > 0)
                {
                    selected.Quantity = qty;
                    selected.Status = statusCombo.SelectedItem?.ToString() ?? "Норма";
                    dialog.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            cancelButton.Click += (s, args) => dialog.DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var context = new BearingProductionEntities())
                    {
                        var record = context.ProductionRecords.Find(selected.RecordId);
                        if (record != null)
                        {
                            record.Quantity = selected.Quantity;
                            record.Status = selected.Status;
                            context.SaveChanges();
                            _recordsView?.Refresh();
                            MessageBox.Show("Запись обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteMyRecord_Click(object sender, RoutedEventArgs e)
        {
            var selected = MyRecordsListView.SelectedItem as ProductionRecords;
            if (selected == null) return;

            if (MessageBox.Show("Удалить запись?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new BearingProductionEntities())
                    {
                        var record = context.ProductionRecords.Find(selected.RecordId);
                        if (record != null)
                        {
                            context.ProductionRecords.Remove(record);
                            context.SaveChanges();
                            _allMyRecords.Remove(selected);
                            MessageBox.Show("Запись удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region Отчеты

        private void MyStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var records = context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.UserId == _userId)
                        .ToList();

                    if (records.Any())
                    {
                        var stats = new
                        {
                            Всего_записей = records.Count(),
                            Всего_произведено = records.Sum(r => r.Quantity),
                            Общая_стоимость = records.Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0)),
                            Среднее_за_запись = Math.Round(records.Average(r => (double)r.Quantity), 1),
                            Первая_запись = records.Min(r => r.ProductionDate),
                            Последняя_запись = records.Max(r => r.ProductionDate)
                        };

                        MyReportTitle.Text = $"Моя статистика: {_workerName}";
                        MyReportGrid.ItemsSource = new[] { stats };
                    }
                    else
                    {
                        MyReportTitle.Text = "Нет данных";
                        MyReportGrid.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MyDaily_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var today = DateTime.Today;
                    var daily = context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.UserId == _userId && r.ProductionDate >= today)
                        .OrderBy(r => r.ProductionDate)
                        .ToList()
                        .Select(r => new
                        {
                            Время = r.ProductionDate.ToString("HH:mm"),
                            Подшипник = r.BearingTypes?.TypeName ?? "",
                            Количество = r.Quantity,
                            Стоимость = r.Quantity * (r.BearingTypes?.Price ?? 0),
                            Статус = r.Status ?? "Норма"
                        })
                        .ToList();

                    MyReportTitle.Text = $"Ежедневный отчет за {today:dd.MM.yyyy}";
                    MyReportGrid.ItemsSource = daily;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MyMonthly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                    var monthly = context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.UserId == _userId && r.ProductionDate >= monthStart)
                        .ToList()
                        .GroupBy(r => r.ProductionDate.Date)
                        .Select(g => new
                        {
                            Дата = g.Key.ToString("dd.MM.yyyy"),
                            Количество_записей = g.Count(),
                            Произведено = g.Sum(r => r.Quantity),
                            Стоимость = g.Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0))
                        })
                        .OrderBy(r => r.Дата)
                        .ToList();

                    MyReportTitle.Text = $"Месячный отчет за {monthStart:MMMM yyyy}";
                    MyReportGrid.ItemsSource = monthly;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompareWithOthers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                    var allData = context.ProductionRecords
                        .Include("Users")
                        .Include("BearingTypes")
                        .Where(r => r.ProductionDate >= monthStart)
                        .ToList();

                    var comparison = allData
                        .GroupBy(r => r.Users?.Name ?? "Неизвестно")
                        .Select(g => new
                        {
                            Рабочий = g.Key,
                            Произведено = g.Sum(r => r.Quantity),
                            Стоимость = g.Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0)),
                            Записей = g.Count(),
                            Среднее_в_день = g.GroupBy(r => r.ProductionDate.Date).Count() > 0
                                ? Math.Round((double)g.Sum(r => r.Quantity) / g.GroupBy(r => r.ProductionDate.Date).Count(), 1)
                                : 0
                        })
                        .OrderByDescending(r => r.Произведено)
                        .ToList();

                    MyReportTitle.Text = "Сравнение с коллегами за текущий месяц";
                    MyReportGrid.ItemsSource = comparison;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMyReport_Click(object sender, RoutedEventArgs e)
        {
            if (MyReportGrid.ItemsSource == null)
            {
                MessageBox.Show("Нет данных для экспорта!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Excel.Application excel = new Excel.Application();
                excel.Visible = true;
                Excel.Workbook workbook = excel.Workbooks.Add();
                Excel.Worksheet sheet = (Excel.Worksheet)workbook.Sheets[1];

                sheet.Cells[1, 1] = MyReportTitle.Text;

                int row = 3;
                int col = 1;
                bool headersWritten = false;

                foreach (var item in MyReportGrid.ItemsSource)
                {
                    var properties = item.GetType().GetProperties();

                    if (!headersWritten)
                    {
                        foreach (var prop in properties)
                        {
                            sheet.Cells[row, col] = prop.Name;
                            col++;
                        }
                        headersWritten = true;
                        row++;
                        col = 1;
                    }

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(item);
                        sheet.Cells[row, col] = value?.ToString() ?? "";
                        col++;
                    }
                    row++;
                    col = 1;
                }

                sheet.Columns.AutoFit();
                MessageBox.Show("Отчет экспортирован в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintMyReport_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(MyReportGrid, MyReportTitle.Text);
            }
        }

        #endregion

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.MainFrame.Navigate(new LoginPage());
        }
    }
}