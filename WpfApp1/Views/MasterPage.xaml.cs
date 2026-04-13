// Views/MasterPage.xaml.cs (исправленная версия)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BearingProductionApp.Helpers;
using Excel = Microsoft.Office.Interop.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using WpfApp1;

namespace BearingProductionApp.Views
{
    public partial class MasterPage : Page
    {
        private ObservableCollection<BearingTypes> _allBearings;
        private ObservableCollection<ProductionRecords> _allProduction;
        private ListCollectionView _bearingsView;
        private ListCollectionView _productionView;

        public ObservableCollection<BearingTypes> BearingTypes { get; set; }
        public ObservableCollection<ProductionRecords> ProductionRecords { get; set; }
        public BearingTypes SelectedBearingType { get; set; }
        public ProductionRecords SelectedProductionRecord { get; set; }
        public DateTime FilterDateFrom { get; set; } = DateTime.Today.AddDays(-7);
        public DateTime FilterDateTo { get; set; } = DateTime.Today;

        public MasterPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    _allBearings = new ObservableCollection<BearingTypes>(
                        context.BearingTypes.Where(b => b.IsActive == true).ToList());
                    BearingTypes = _allBearings;
                    BearingsListView.ItemsSource = BearingTypes;
                    _bearingsView = (ListCollectionView)CollectionViewSource.GetDefaultView(BearingTypes);
                    _bearingsView.SortDescriptions.Add(new SortDescription("TypeName", ListSortDirection.Ascending));

                    _allProduction = new ObservableCollection<ProductionRecords>(
                        context.ProductionRecords
                            .Include("Users")
                            .Include("BearingTypes")
                            .OrderByDescending(p => p.ProductionDate)
                            .ToList());
                    ProductionRecords = _allProduction;
                    ProductionListView.ItemsSource = ProductionRecords;
                    _productionView = (ListCollectionView)CollectionViewSource.GetDefaultView(ProductionRecords);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Управление подшипниками

        private void AddBearing_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BearingNameTextBox.Text))
            {
                MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(BearingPriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var newBearing = new BearingTypes
                    {
                        TypeName = BearingNameTextBox.Text,
                        Price = price,
                        IsActive = true
                    };
                    context.BearingTypes.Add(newBearing);
                    context.SaveChanges();

                    _allBearings.Add(newBearing);
                    BearingNameTextBox.Text = "";
                    BearingPriceTextBox.Text = "";

                    MessageBox.Show("Подшипник добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBearing_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBearingType == null)
            {
                MessageBox.Show("Выберите подшипник!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var bearing = context.BearingTypes.Find(SelectedBearingType.BearingTypeId);
                    if (bearing != null)
                    {
                        bearing.TypeName = SelectedBearingType.TypeName;
                        bearing.Price = SelectedBearingType.Price;
                        context.SaveChanges();
                        _bearingsView?.Refresh();
                        MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArchiveBearing_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBearingType == null)
            {
                MessageBox.Show("Выберите подшипник!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Переместить в архив?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new BearingProductionEntities())
                    {
                        var bearing = context.BearingTypes.Find(SelectedBearingType.BearingTypeId);
                        if (bearing != null)
                        {
                            bearing.IsActive = false;
                            context.SaveChanges();
                            _allBearings.Remove(SelectedBearingType);
                            MessageBox.Show("Перемещено в архив!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SearchBearing_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_bearingsView != null)
            {
                string searchText = SearchBearingBox.Text.ToLower();
                _bearingsView.Filter = item =>
                {
                    var bearing = item as BearingTypes;
                    return bearing != null && bearing.TypeName.ToLower().Contains(searchText);
                };
            }
        }

        private void ShowArchived_Changed(object sender, RoutedEventArgs e)
        {
            if (ShowArchivedCheckBox.IsChecked == true)
            {
                using (var context = new BearingProductionEntities())
                {
                    BearingTypes = new ObservableCollection<BearingTypes>(context.BearingTypes.ToList());
                }
            }
            else
            {
                BearingTypes = _allBearings;
            }
            BearingsListView.ItemsSource = BearingTypes;
            _bearingsView = (ListCollectionView)CollectionViewSource.GetDefaultView(BearingTypes);
        }

        private void BearingSort_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_bearingsView == null) return;

            _bearingsView.SortDescriptions.Clear();

            var selected = (BearingSortCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (selected)
            {
                case "По названию":
                    _bearingsView.SortDescriptions.Add(new SortDescription("TypeName", ListSortDirection.Ascending));
                    break;
                case "По цене (возр)":
                    _bearingsView.SortDescriptions.Add(new SortDescription("Price", ListSortDirection.Ascending));
                    break;
                case "По цене (убыв)":
                    _bearingsView.SortDescriptions.Add(new SortDescription("Price", ListSortDirection.Descending));
                    break;
                case "По ID":
                    _bearingsView.SortDescriptions.Add(new SortDescription("BearingTypeId", ListSortDirection.Ascending));
                    break;
            }

            _bearingsView.Refresh();
        }

        #endregion

        #region Управление производством

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyProductionFilters();
        }

        private void SearchProduction_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyProductionFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            DateFromPicker.SelectedDate = DateTime.Today.AddDays(-7);
            DateToPicker.SelectedDate = DateTime.Today;
            StatusFilterCombo.SelectedIndex = 0;
            SearchProductionBox.Text = "";
            ApplyProductionFilters();
        }

        private void ApplyProductionFilters()
        {
            if (_productionView == null) return;

            _productionView.Filter = item =>
            {
                var record = item as ProductionRecords;
                if (record == null) return false;

                if (DateFromPicker.SelectedDate.HasValue && record.ProductionDate < DateFromPicker.SelectedDate.Value)
                    return false;
                if (DateToPicker.SelectedDate.HasValue && record.ProductionDate > DateToPicker.SelectedDate.Value.AddDays(1))
                    return false;

                var selectedStatus = (StatusFilterCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (selectedStatus != "Все" && record.Status != selectedStatus)
                    return false;

                string searchText = SearchProductionBox.Text.ToLower();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    return (record.Users?.Name?.ToLower().Contains(searchText) ?? false) ||
                           (record.BearingTypes?.TypeName?.ToLower().Contains(searchText) ?? false);
                }

                return true;
            };

            _productionView.Refresh();
        }

        private void ProductionGroup_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_productionView == null) return;

            _productionView.GroupDescriptions.Clear();

            var selected = (ProductionGroupCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (selected)
            {
                case "По статусу":
                    _productionView.GroupDescriptions.Add(new PropertyGroupDescription("Status"));
                    break;
                case "По рабочему":
                    _productionView.GroupDescriptions.Add(new PropertyGroupDescription("Users.Name"));
                    break;
                case "По дате":
                    _productionView.GroupDescriptions.Add(new PropertyGroupDescription("ProductionDate"));
                    break;
            }

            _productionView.Refresh();
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_productionView == null) return;

            _productionView.SortDescriptions.Clear();
            _productionView.CustomSort = null;

            var selected = (SortCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            switch (selected)
            {
                case "По дате (новые)":
                    _productionView.SortDescriptions.Add(new SortDescription("ProductionDate", ListSortDirection.Descending));
                    break;
                case "По дате (старые)":
                    _productionView.SortDescriptions.Add(new SortDescription("ProductionDate", ListSortDirection.Ascending));
                    break;
                case "По количеству":
                    _productionView.SortDescriptions.Add(new SortDescription("Quantity", ListSortDirection.Descending));
                    break;
                case "По стоимости":
                    _productionView.CustomSort = new TotalCostComparer(ListSortDirection.Descending);
                    break;
                case "По статусу":
                    _productionView.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Ascending));
                    break;
            }

            _productionView.Refresh();
        }

        public class TotalCostComparer : IComparer
        {
            private ListSortDirection _direction;

            public TotalCostComparer(ListSortDirection direction)
            {
                _direction = direction;
            }

            public int Compare(object x, object y)
            {
                var record1 = x as ProductionRecords;
                var record2 = y as ProductionRecords;

                if (record1 == null || record2 == null) return 0;

                decimal cost1 = record1.BearingTypes != null ? record1.Quantity * record1.BearingTypes.Price : 0;
                decimal cost2 = record2.BearingTypes != null ? record2.Quantity * record2.BearingTypes.Price : 0;

                int result = cost1.CompareTo(cost2);

                if (_direction == ListSortDirection.Descending)
                    result = -result;

                return result;
            }
        }

        private void RefreshProduction_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
        }

        private void EditProduction_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProductionRecord == null)
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
            var quantityBox = new TextBox { Text = SelectedProductionRecord.Quantity.ToString(), Margin = new Thickness(0, 5, 0, 10), Height = 25 };
            stackPanel.Children.Add(quantityBox);

            stackPanel.Children.Add(new TextBlock { Text = "Статус:", FontWeight = FontWeights.Bold });
            var statusCombo = new ComboBox { Margin = new Thickness(0, 5, 0, 15), Height = 25 };
            statusCombo.Items.Add("Отлично");
            statusCombo.Items.Add("Хорошо");
            statusCombo.Items.Add("Норма");
            statusCombo.SelectedItem = SelectedProductionRecord.Status;
            stackPanel.Children.Add(statusCombo);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Width = 80, Margin = new Thickness(5), Height = 28 };
            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5), Height = 28 };

            okButton.Click += (s, args) =>
            {
                if (int.TryParse(quantityBox.Text, out int qty) && qty > 0)
                {
                    SelectedProductionRecord.Quantity = qty;
                    SelectedProductionRecord.Status = statusCombo.SelectedItem?.ToString() ?? "Норма";
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
                        var record = context.ProductionRecords.Find(SelectedProductionRecord.RecordId);
                        if (record != null)
                        {
                            record.Quantity = SelectedProductionRecord.Quantity;
                            record.Status = SelectedProductionRecord.Status;
                            context.SaveChanges();
                            _productionView?.Refresh();
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

        private void ArchiveProduction_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProductionRecord == null) return;

            if (MessageBox.Show("Переместить запись в архив?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new BearingProductionEntities())
                    {
                        var archive = new ProductionRecordsArchive
                        {
                            RecordId = SelectedProductionRecord.RecordId,
                            UserId = SelectedProductionRecord.UserId,
                            BearingTypeId = SelectedProductionRecord.BearingTypeId,
                            Quantity = SelectedProductionRecord.Quantity,
                            ProductionDate = SelectedProductionRecord.ProductionDate,
                            Status = SelectedProductionRecord.Status
                        };
                        context.ProductionRecordsArchive.Add(archive);

                        var record = context.ProductionRecords.Find(SelectedProductionRecord.RecordId);
                        context.ProductionRecords.Remove(record);

                        context.SaveChanges();
                        _allProduction.Remove(SelectedProductionRecord);
                        _productionView?.Refresh();
                        MessageBox.Show("Запись перемещена в архив!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetStatusExcellent_Click(object sender, RoutedEventArgs e) => ChangeStatus("Отлично");
        private void SetStatusGood_Click(object sender, RoutedEventArgs e) => ChangeStatus("Хорошо");
        private void SetStatusNormal_Click(object sender, RoutedEventArgs e) => ChangeStatus("Норма");

        private void ChangeStatus(string newStatus)
        {
            if (SelectedProductionRecord == null) return;

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var record = context.ProductionRecords.Find(SelectedProductionRecord.RecordId);
                    if (record != null)
                    {
                        record.Status = newStatus;
                        context.SaveChanges();
                        SelectedProductionRecord.Status = newStatus;
                        _productionView?.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Экспорт

        private void ExportBearingsToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Excel.Application excel = new Excel.Application();
                excel.Visible = true;
                Excel.Workbook workbook = excel.Workbooks.Add();
                Excel.Worksheet sheet = (Excel.Worksheet)workbook.Sheets[1];

                sheet.Cells[1, 1] = "Типы подшипников";
                sheet.Cells[2, 1] = "ID";
                sheet.Cells[2, 2] = "Название";
                sheet.Cells[2, 3] = "Цена";
                sheet.Cells[2, 4] = "Статус";

                int row = 3;
                foreach (var bearing in BearingTypes)
                {
                    sheet.Cells[row, 1] = bearing.BearingTypeId;
                    sheet.Cells[row, 2] = bearing.TypeName;
                    sheet.Cells[row, 3] = bearing.Price;
                    sheet.Cells[row, 4] = bearing.IsActive == true ? "Активен" : "В архиве";
                    row++;
                }

                sheet.Columns.AutoFit();
                MessageBox.Show("Данные экспортированы в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportProductionToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Excel.Application excel = new Excel.Application();
                excel.Visible = true;
                Excel.Workbook workbook = excel.Workbooks.Add();
                Excel.Worksheet sheet = (Excel.Worksheet)workbook.Sheets[1];

                sheet.Cells[1, 1] = "Производственные записи";
                string[] headers = { "Дата", "Рабочий", "Тип подшипника", "Количество", "Цена", "Стоимость", "Статус" };
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.Cells[2, i + 1] = headers[i];
                }

                int row = 3;
                decimal totalCost = 0;
                foreach (ProductionRecords record in ProductionListView.Items)
                {
                    decimal cost = record.Quantity * (record.BearingTypes?.Price ?? 0);
                    sheet.Cells[row, 1] = record.ProductionDate.ToString("dd.MM.yyyy HH:mm");
                    sheet.Cells[row, 2] = record.Users?.Name ?? "";
                    sheet.Cells[row, 3] = record.BearingTypes?.TypeName ?? "";
                    sheet.Cells[row, 4] = record.Quantity;
                    sheet.Cells[row, 5] = record.BearingTypes?.Price ?? 0;
                    sheet.Cells[row, 6] = cost;
                    sheet.Cells[row, 7] = record.Status;
                    totalCost += cost;
                    row++;
                }

                sheet.Cells[row, 5] = "ИТОГО:";
                sheet.Cells[row, 6] = totalCost;
                sheet.Range["A" + row + ":G" + row].Font.Bold = true;

                sheet.Columns.AutoFit();
                MessageBox.Show("Данные экспортированы в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportProductionToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "PDF files (*.pdf)|*.pdf";
                saveDialog.DefaultExt = ".pdf";
                saveDialog.FileName = "ProductionReport.pdf";

                if (saveDialog.ShowDialog() == true)
                {
                    using (FileStream fs = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        Document document = new Document(PageSize.A4.Rotate());
                        PdfWriter.GetInstance(document, fs);
                        document.Open();

                        iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                        Paragraph title = new Paragraph("Производственные записи", titleFont);
                        title.Alignment = Element.ALIGN_CENTER;
                        document.Add(title);
                        document.Add(new Paragraph(" "));

                        PdfPTable table = new PdfPTable(7);
                        table.WidthPercentage = 100;

                        string[] headers = { "Дата", "Рабочий", "Тип", "Кол-во", "Цена", "Стоимость", "Статус" };
                        foreach (string header in headers)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(header, FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.BOLD)));
                            cell.BackgroundColor = new BaseColor(240, 240, 240);
                            table.AddCell(cell);
                        }

                        decimal totalCost = 0;
                        foreach (ProductionRecords record in ProductionListView.Items)
                        {
                            decimal cost = record.Quantity * (record.BearingTypes?.Price ?? 0);

                            table.AddCell(record.ProductionDate.ToString("dd.MM.yyyy HH:mm"));
                            table.AddCell(record.Users?.Name ?? "");
                            table.AddCell(record.BearingTypes?.TypeName ?? "");
                            table.AddCell(record.Quantity.ToString());
                            table.AddCell((record.BearingTypes?.Price ?? 0).ToString("F2"));
                            table.AddCell(cost.ToString("F2"));
                            table.AddCell(record.Status ?? "");

                            totalCost += cost;
                        }

                        document.Add(table);
                        document.Add(new Paragraph(" "));

                        Paragraph total = new Paragraph($"ИТОГО: {totalCost:F2} ₽",
                            FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD));
                        total.Alignment = Element.ALIGN_RIGHT;
                        document.Add(total);

                        document.Close();
                    }

                    MessageBox.Show($"Отчет сохранен: {saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(ProductionListView, "Производственные записи");
                }
            }
        }

        private void PrintBearings_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(BearingsListView, "Типы подшипников");
            }
        }

        private void PrintProduction_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ProductionListView, "Производственные записи");
            }
        }

        #endregion

        #region Отчеты

        private void DailyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var today = DateTime.Today;
                    var records = context.ProductionRecords
                        .Include("Users")
                        .Include("BearingTypes")
                        .Where(r => r.ProductionDate >= today)
                        .ToList();

                    var reportData = records.Select(r => new
                    {
                        Время = r.ProductionDate.ToString("HH:mm"),
                        Рабочий = r.Users.Name,
                        Подшипник = r.BearingTypes.TypeName,
                        Количество = r.Quantity,
                        Стоимость = r.Quantity * r.BearingTypes.Price,
                        Статус = r.Status
                    }).ToList();

                    ReportTitleBlock.Text = $"Ежедневная выписка за {today:dd.MM.yyyy}";
                    ReportDataGrid.ItemsSource = reportData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurrentStatusReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var today = DateTime.Today;
                    var weekStart = today.AddDays(-(int)today.DayOfWeek);

                    var todayQuantity = context.ProductionRecords
                        .Where(r => r.ProductionDate >= today)
                        .Sum(r => (int?)r.Quantity) ?? 0;

                    var todayCost = context.ProductionRecords
                        .Where(r => r.ProductionDate >= today)
                        .ToList()
                        .Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0));

                    var todayCount = context.ProductionRecords
                        .Count(r => r.ProductionDate >= today);

                    var weekQuantity = context.ProductionRecords
                        .Where(r => r.ProductionDate >= weekStart)
                        .Sum(r => (int?)r.Quantity) ?? 0;

                    var weekCost = context.ProductionRecords
                        .Where(r => r.ProductionDate >= weekStart)
                        .ToList()
                        .Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0));

                    var weekCount = context.ProductionRecords
                        .Count(r => r.ProductionDate >= weekStart);

                    var reportData = new[]
                    {
                        new { Период = "Сегодня", Количество = todayQuantity, Стоимость = todayCost, Записей = todayCount },
                        new { Период = "За неделю", Количество = weekQuantity, Стоимость = weekCost, Записей = weekCount }
                    };

                    ReportTitleBlock.Text = "Текущий статус производства";
                    ReportDataGrid.ItemsSource = reportData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RatingReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var rating = context.ProductionRecords
                        .Include("Users")
                        .Include("BearingTypes")
                        .ToList()
                        .GroupBy(r => r.Users?.Name ?? "Неизвестно")
                        .Select(g => new
                        {
                            Рабочий = g.Key,
                            Всего_штук = g.Sum(r => r.Quantity),
                            Общая_стоимость = g.Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0)),
                            Количество_записей = g.Count(),
                            Среднее_за_запись = Math.Round(g.Average(r => (double)r.Quantity), 1)
                        })
                        .OrderByDescending(r => r.Общая_стоимость)
                        .ToList();

                    ReportTitleBlock.Text = "Рейтинг рабочих по производительности";
                    ReportDataGrid.ItemsSource = rating;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SummaryReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var summary = context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.BearingTypes != null)
                        .ToList()
                        .GroupBy(r => r.BearingTypes?.TypeName ?? "Неизвестно")
                        .Select(g => new
                        {
                            Тип_подшипника = g.Key,
                            Произведено = g.Sum(r => r.Quantity),
                            Общая_стоимость = g.Sum(r => r.Quantity * (r.BearingTypes?.Price ?? 0)),
                            Средняя_партия = Math.Round(g.Average(r => (double)r.Quantity), 1),
                            Минимальная = g.Min(r => r.Quantity),
                            Максимальная = g.Max(r => r.Quantity)
                        })
                        .OrderByDescending(r => r.Произведено)
                        .ToList();

                    ReportTitleBlock.Text = "Сводка по типам подшипников";
                    ReportDataGrid.ItemsSource = summary;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WorkerReport_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProductionRecord?.Users != null)
            {
                ShowWorkerReport(SelectedProductionRecord.Users.Name);
            }
            else
            {
                using (var context = new BearingProductionEntities())
                {
                    var workers = context.Users.Where(u => u.IdRole == 2).ToList();
                    if (workers.Any())
                    {
                        var dialog = new WorkerSelectionDialog(workers);
                        if (dialog.ShowDialog() == true && dialog.SelectedWorker != null)
                        {
                            ShowWorkerReport(dialog.SelectedWorker.Name);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Нет рабочих в базе!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ShowWorkerReport(string workerName)
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var workerRecords = context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.Users.Name == workerName)
                        .OrderByDescending(r => r.ProductionDate)
                        .Select(r => new
                        {
                            Дата = r.ProductionDate,
                            Подшипник = r.BearingTypes.TypeName,
                            Количество = r.Quantity,
                            Стоимость = r.Quantity * r.BearingTypes.Price,
                            Статус = r.Status
                        })
                        .ToList();

                    ReportTitleBlock.Text = $"Отчет по рабочему: {workerName}";
                    ReportDataGrid.ItemsSource = workerRecords;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportReportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (ReportDataGrid.ItemsSource == null)
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

                sheet.Cells[1, 1] = ReportTitleBlock.Text;

                int row = 3;
                int col = 1;
                bool headersWritten = false;

                foreach (var item in ReportDataGrid.ItemsSource)
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

        private void ExportReportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (ReportDataGrid.ItemsSource == null)
            {
                MessageBox.Show("Нет данных для экспорта!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "PDF files (*.pdf)|*.pdf";
                saveDialog.DefaultExt = ".pdf";
                saveDialog.FileName = "Report.pdf";

                if (saveDialog.ShowDialog() == true)
                {
                    using (FileStream fs = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        Document document = new Document(PageSize.A4.Rotate());
                        PdfWriter.GetInstance(document, fs);
                        document.Open();

                        iTextSharp.text.Font titleFont = FontFactory.GetFont("Arial", 16, iTextSharp.text.Font.BOLD);
                        Paragraph title = new Paragraph(ReportTitleBlock.Text, titleFont);
                        title.Alignment = Element.ALIGN_CENTER;
                        document.Add(title);
                        document.Add(new Paragraph(" "));

                        var items = ReportDataGrid.ItemsSource.Cast<object>().ToList();
                        if (items.Any())
                        {
                            var properties = items.First().GetType().GetProperties();

                            PdfPTable table = new PdfPTable(properties.Length);
                            table.WidthPercentage = 100;

                            foreach (var prop in properties)
                            {
                                PdfPCell cell = new PdfPCell(new Phrase(prop.Name,
                                    FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.BOLD)));
                                cell.BackgroundColor = new BaseColor(240, 240, 240);
                                table.AddCell(cell);
                            }

                            foreach (var item in items)
                            {
                                foreach (var prop in properties)
                                {
                                    var value = prop.GetValue(item);
                                    table.AddCell(value?.ToString() ?? "");
                                }
                            }

                            document.Add(table);
                        }

                        document.Close();
                    }

                    MessageBox.Show($"Отчет сохранен: {saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(ReportDataGrid, ReportTitleBlock.Text);
                }
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(ReportDataGrid, ReportTitleBlock.Text);
            }
        }

        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.MainFrame.Navigate(new LoginPage());
        }
    }

    public class WorkerSelectionDialog : Window
    {
        public Users SelectedWorker { get; private set; }
        private ListView workerList;

        public WorkerSelectionDialog(List<Users> workers)
        {
            Title = "Выберите рабочего";
            Width = 300;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            workerList = new ListView
            {
                Margin = new Thickness(10),
                ItemsSource = workers,
                DisplayMemberPath = "Name"
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var okButton = new Button { Content = "Выбрать", Width = 80, Margin = new Thickness(5), Height = 28 };
            okButton.Click += (s, e) =>
            {
                SelectedWorker = workerList.SelectedItem as Users;
                DialogResult = SelectedWorker != null;
            };

            var cancelButton = new Button { Content = "Отмена", Width = 80, Margin = new Thickness(5), Height = 28 };
            cancelButton.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(workerList, 0);
            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(workerList);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}