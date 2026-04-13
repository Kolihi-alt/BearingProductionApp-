using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BearingProductionApp.Helpers;
using WpfApp1;

namespace BearingProductionApp.ViewModels
{
    public class WorkerViewModel : BaseViewModel
    {
        private ObservableCollection<BearingTypes> _bearingTypes;
        private ObservableCollection<ProductionRecords> _allProductionRecords;
        private ObservableCollection<ProductionRecords> _productionRecords;
        private BearingTypes _selectedBearingType;
        private int _quantity;
        private readonly int _userId;
        private string _searchBearingType;

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

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public string SearchBearingType
        {
            get => _searchBearingType;
            set
            {
                _searchBearingType = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public WorkerViewModel(int userId)
        {
            _userId = userId;
            BearingTypes = new ObservableCollection<BearingTypes>();
            _allProductionRecords = new ObservableCollection<ProductionRecords>();
            ProductionRecords = new ObservableCollection<ProductionRecords>();
            LoadBearingTypes();
            LoadProductionRecords();
        }

        private void LoadBearingTypes()
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    BearingTypes.Clear();
                    foreach (var type in context.BearingTypes.ToList())
                        BearingTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов подшипников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductionRecords()
        {
            try
            {
                using (var context = new BearingProductionEntities())
                {
                    _allProductionRecords.Clear();
                    foreach (var record in context.ProductionRecords
                        .Include("BearingTypes")
                        .Where(r => r.UserId == _userId)
                        .ToList())
                    {
                        _allProductionRecords.Add(record);
                    }
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            var filteredRecords = _allProductionRecords.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchBearingType))
                filteredRecords = filteredRecords.Where(r => r.BearingTypes.TypeName.ToLower().Contains(SearchBearingType.ToLower()));

            ProductionRecords.Clear();
            foreach (var record in filteredRecords)
                ProductionRecords.Add(record);
        }

        public void AddProductionRecord()
        {
            if (SelectedBearingType == null || Quantity <= 0)
            {
                MessageBox.Show("Выберите тип подшипника и укажите количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new BearingProductionEntities())
                {
                    var record = new ProductionRecords
                    {
                        UserId = _userId,
                        BearingTypeId = SelectedBearingType.BearingTypeId,
                        Quantity = Quantity,
                        ProductionDate = DateTime.Now
                    };
                    context.ProductionRecords.Add(record);
                    context.SaveChanges();
                    Quantity = 0;
                    LoadProductionRecords();
                    MessageBox.Show("Запись добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}