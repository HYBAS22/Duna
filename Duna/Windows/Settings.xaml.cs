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
            Debug.WriteLine(MainWindow.milisecondForScreenshotDelay.ToString());
            DelayForScreenshotBar.Text = MainWindow.milisecondForScreenshotDelay.ToString();
        }

        private async void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (checkArgsAfterMainWord.IsChecked == true) MainWindow.isCheckMainWord = true;
            else MainWindow.isCheckMainWord = false;

            await SaveData(MainWindow.isCheckMainWord, MainWindow.showArgsInfo, MainWindow.milisecondForScreenshotDelay);
        }

        private async void CheckArgs_Changed(object sender, RoutedEventArgs e)
        {
            if (checkArgsInfo.IsChecked == true) MainWindow.showArgsInfo = true;
            else MainWindow.showArgsInfo = false;

            await SaveData(MainWindow.isCheckMainWord, MainWindow.showArgsInfo, MainWindow.milisecondForScreenshotDelay);
        }

        async Task SaveData(bool mainWord, bool argInfo, int delayScreenshot)
        {
            var rootDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Duna");

            using var repository = new SaveSystem.SaveRepository(rootDirectory, "config.bin");
            SaveData data = await repository.Load();

            data.checkMainWord = mainWord;
            data.showArgsInfo = argInfo;
            data.delayForScreenshot = delayScreenshot;

            await repository.Save(data);
            Debug.WriteLine($"данные сохранены в - {rootDirectory}");
        }

        private async void DelayForScreenshotBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(DelayForScreenshotBar.Text, out MainWindow.milisecondForScreenshotDelay);

            if (MainWindow.milisecondForScreenshotDelay > 5000 || MainWindow.milisecondForScreenshotDelay < 0)
            {
                MessageBox.Show("Значение превышает лимит, будет установлено 350!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                MainWindow.milisecondForScreenshotDelay = 350;
                DelayForScreenshotBar.Text = "350";
            }

            await SaveData(MainWindow.isCheckMainWord, MainWindow.showArgsInfo, MainWindow.milisecondForScreenshotDelay);
        }
    }
}
