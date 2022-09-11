using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Duna.SaveModules;

namespace Duna.Windows
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();

            checkArgsAfterMainWord.IsChecked = MainWindow.isCheckMainWord;
            checkArgsInfo.IsChecked = MainWindow.showArgsInfo;
        }

        private async void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (checkArgsAfterMainWord.IsChecked == true) MainWindow.isCheckMainWord = true;
            else MainWindow.isCheckMainWord = false;

            await SaveData(MainWindow.isCheckMainWord, MainWindow.showArgsInfo);
        }

        private async void CheckArgs_Changed(object sender, RoutedEventArgs e)
        {
            if (checkArgsInfo.IsChecked == true) MainWindow.showArgsInfo = true;
            else MainWindow.showArgsInfo = false;

            await SaveData(MainWindow.isCheckMainWord, MainWindow.showArgsInfo);
        }

        async Task SaveData(bool mainWord, bool argInfo)
        {
            var rootDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Duna");

            using var repository = new SaveSystem.SaveRepository(rootDirectory, "config.bin");
            SaveData data = await repository.Load();

            data.checkMainWord = mainWord;
            data.showArgsInfo = argInfo;

            await repository.Save(data);
            Debug.WriteLine($"данные сохранены в - {rootDirectory}");
        }
    }
}
