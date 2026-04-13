using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using Excel = Microsoft.Office.Interop.Excel;
using BearingProductionApp.Helpers;
using WpfApp1;

namespace BearingProductionApp.ViewModels
{
    public class MasterViewModel : BaseViewModel
    {
        private ObservableCollection<BearingTypes> _bearingTypes;
        private ObservableCollection<ProductionRecords> _allProductionRecords; // Хранит все записи
        private ObservableCollection<ProductionRecords> _productionRecords;    // Фильтруемая коллекция
        private BearingTypes _selectedBearingType;
        private string _newTypeName;
        private decimal _newTypePrice;
        private DateTime _filterDate;
        private string _searchWorkerName;

        public ObservableCollection<BearingTypes> BearingTypes
        {
            get => _bearingTypes;
            set
            {
                _bearingTypes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProductionRecords> ProductionRecords
        {
            get => _productionRecords;
            set
            {
                _productionRecords = value;
                OnPropertyChanged();
            }
        }

        public BearingTypes SelectedBearingType
        {
            get => _selectedBearingType;
            set
            {
                _selectedBearingType = value;
                OnPropertyChanged();
            }
        }

        public string NewTypeName
        {
            get => _newTypeName;
            set
            {
                _newTypeName = value;
                OnPropertyChanged();
            }
        }

        public decimal NewTypePrice
        {
            get => _newTypePrice;
            set
            {
                _newTypePrice = value;
                OnPropertyChanged();
            }
        }

        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                _filterDate = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string SearchWorkerName
        {
            get => _searchWorkerName;
            set
            {
                _searchWorkerName = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public MasterViewModel()
        {
            BearingTypes = new ObservableCollection<BearingTypes>();
            _allProductionRecords = new ObservableCollection<ProductionRecords>();
            ProductionRecords = new ObservableCollection<ProductionRecords>();
            _filterDate = DateTime.Now;
            LoadData();
        }

        internal void LoadData()
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    BearingTypes.Clear();
                    foreach (var type in context.BearingTypes.ToList())
                        BearingTypes.Add(type);

                    _allProductionRecords.Clear();
                    foreach (var record in context.ProductionRecords.Include("Users").Include("BearingTypes").ToList())
                        _allProductionRecords.Add(record);

                    ApplyFilters(); // Применяем фильтры после загрузки
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filteredRecords = _allProductionRecords.AsEnumerable();
            if (FilterDate != null && FilterDate != DateTime.MinValue)
                filteredRecords = filteredRecords.Where(r => r.ProductionDate.Date == FilterDate.Date);
            if (!string.IsNullOrWhiteSpace(SearchWorkerName))
                filteredRecords = filteredRecords.Where(r => r.Users.Name.ToLower().Contains(SearchWorkerName.ToLower()));

            ProductionRecords.Clear();
            foreach (var record in filteredRecords)
                ProductionRecords.Add(record);
        }

        public void AddBearingType()
        {
            if (string.IsNullOrWhiteSpace(NewTypeName) || NewTypePrice <= 0)
            {
                MessageBox.Show("Введите корректное название и цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var newType = new BearingTypes { TypeName = NewTypeName, Price = NewTypePrice };
                    context.BearingTypes.Add(newType);
                    context.SaveChanges();
                    BearingTypes.Add(newType);
                    NewTypeName = string.Empty;
                    NewTypePrice = 0;
                    MessageBox.Show("Тип подшипника добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateBearingType()
        {
            if (SelectedBearingType == null)
            {
                MessageBox.Show("Выберите подшипник для редактирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var type = context.BearingTypes.Find(SelectedBearingType.BearingTypeId);
                    if (type != null)
                    {
                        type.TypeName = SelectedBearingType.TypeName;
                        type.Price = SelectedBearingType.Price;
                        context.SaveChanges();
                        LoadData();
                        MessageBox.Show("Тип подшипника обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void DeleteBearingType()
        {
            if (SelectedBearingType == null)
            {
                MessageBox.Show("Выберите тип подшипника для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var type = context.BearingTypes.Find(SelectedBearingType.BearingTypeId);
                    if (type != null)
                    {
                        context.BearingTypes.Remove(type);
                        context.SaveChanges();
                        BearingTypes.Remove(SelectedBearingType);
                        MessageBox.Show("Тип подшипника удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToExcel()
        {
            try
            {
                if (!ProductionRecords.Any())
                {
                    MessageBox.Show("Нет данных для экспорта!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Excel.Application excel = new Excel.Application();
                try
                {
                    excel.Visible = false;
                    Excel.Workbook workbook = excel.Workbooks.Add();
                    Excel.Worksheet sheet = (Excel.Worksheet)workbook.Sheets[1];

                    sheet.Cells[1, 1] = "Отчет по производству подшипников";
                    sheet.Range["A1:E1"].Merge();
                    sheet.Range["A1"].Font.Size = 16;
                    sheet.Range["A1"].Font.Bold = true;
                    sheet.Range["A1"].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

                    sheet.Cells[2, 1] = "Рабочий";
                    sheet.Cells[2, 2] = "Тип подшипника";
                    sheet.Cells[2, 3] = "Количество";
                    sheet.Cells[2, 4] = "Дата";
                    sheet.Cells[2, 5] = "Стоимость, ₽";
                    sheet.Range["A2:E2"].Font.Bold = true;
                    sheet.Range["A2:E2"].Borders.LineStyle = Excel.XlLineStyle.xlContinuous;

                    int row = 3;
                    decimal totalCost = 0;
                    foreach (var record in ProductionRecords)
                    {
                        decimal cost = record.Quantity * record.BearingTypes.Price;
                        sheet.Cells[row, 1] = record.Users.Name;
                        sheet.Cells[row, 2] = record.BearingTypes.TypeName;
                        sheet.Cells[row, 3] = record.Quantity;
                        sheet.Cells[row, 4] = record.ProductionDate.ToString("dd.MM.yyyy");
                        sheet.Cells[row, 5] = cost.ToString("F2") + " ₽";
                        sheet.Range[$"A{row}:E{row}"].Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
                        totalCost += cost;
                        row++;
                    }

                    sheet.Cells[row, 4] = "Итого:";
                    sheet.Cells[row, 5] = totalCost.ToString("F2") + " ₽";
                    sheet.Range[$"A{row}:E{row}"].Font.Bold = true;

                    sheet.Columns["A:E"].AutoFit();
                    string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ProductionReport.xlsx");
                    workbook.SaveAs(filePath);
                    workbook.Close();
                    MessageBox.Show($"Отчет сохранен: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    excel.Quit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}