using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Duna.Module
{
    public class LogSystem
    {
        public static async void WriteToLogs(string logsText)
        {
            logsText = $"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")} - {logsText}\n\n";

            try
            {
                await File.WriteAllTextAsync("logs.txt", logsText);
            }

            catch
            {
                MessageBox.Show("Произошла ошибка при записи логов", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
