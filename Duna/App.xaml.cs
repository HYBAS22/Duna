using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Duna
{
    /// <summary>
    /// Здесь логика для всякой фигни, ну щяс в основном для иконки
    /// </summary>
    public partial class App : Application
    {
        public void Open_Program(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Show();
        }

        public void Close_Program(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
