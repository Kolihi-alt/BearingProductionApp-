using System;
using System.Data.Entity;
using System.Windows;
using WpfApp1;

namespace BearingProductionApp.Helpers
{
    public static class AppConnect
    {
        public static BearingProductionEntities Model { get; private set; }

        static AppConnect()
        {
            try
            {
                Model = new BearingProductionEntities();
                if (Model.Database.Connection.State == System.Data.ConnectionState.Closed)
                {
                    Model.Database.Connection.Open();
                    Model.Database.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Model = null;
            }
        }

        public static bool IsDatabaseConnected()
        {
            if (Model == null) return false;
            try
            {
                if (Model.Database.Connection.State == System.Data.ConnectionState.Closed)
                {
                    Model.Database.Connection.Open();
                    Model.Database.Connection.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}