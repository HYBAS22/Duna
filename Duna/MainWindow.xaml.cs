using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Data;
using AngleSharp;
using System.Xml;
using Duna.Windows;
using Duna.SaveModules;
using Duna.Module;
using Duna.Programs;
using System.Text.RegularExpressions;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using AngleSharp.Browser;
using System.Windows.Threading;

namespace Duna
{
    /// <summary>
    /// Код чёрт возьми
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string LastText, LastCommand;
        public static bool isCheckMainWord = false;
        public static bool showArgsInfo = false;
        public static int milisecondForScreenshotDelay = 350;
        static Dictionary<int, string> programs;

        public static TaskbarIcon tb { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tb = (TaskbarIcon)FindResource("Duna");

            await LoadSavedData();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Application.Current.MainWindow.Hide();
        }

        private void SendMessageButton(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UserInputText.Text))
            {
                AddNewMessage(true, UserInputText.Text.ToString());
                UserInputText.Text = "";
            }
        }

        async Task LoadSavedData()
        {
            var rootDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Duna");

            using var repository = new SaveSystem.SaveRepository(rootDirectory, "config.bin");
            Debug.WriteLine("Первоначальная загрузка данных и вывод на экран");
            SaveData data = await repository.Load();
            Debug.WriteLine($"mainWord: {data.checkMainWord}");
            Debug.WriteLine($"argsInfo: {data.showArgsInfo}");
            Debug.WriteLine($"screenshotDelayTime: {data.delayForScreenshot}");

            data = await repository.Load();
            isCheckMainWord = data.checkMainWord;
            showArgsInfo = data.showArgsInfo;
            milisecondForScreenshotDelay = data.delayForScreenshot;
            Debug.WriteLine($"вывод после загрузки: {data.checkMainWord}, {data.showArgsInfo}, {data.delayForScreenshot}");
        }

        void AddNewMessage(bool IsUserMessage, string text)
        {
            // Обработка текста, удаление знаков и т.д
            string[] punctuation_marks = { ".", ",", "!", "?", ";", "", "-", "(", ")", "#" };
            string text_without_chars = text;
            Console.WriteLine(text_without_chars);
            int count = 0;

            for (int i = 0; i < text_without_chars.Length; i++)
                for (int j = 0; j < punctuation_marks.Length; j++)
                    if (text_without_chars[i].ToString() == punctuation_marks[j])
                        count++;

            for (int j = 0; j < punctuation_marks.Length; j++)
                try { text_without_chars = text_without_chars.Replace(punctuation_marks[j], ""); }
                catch { };

            // разбивка предложения на слова
            string[] chunks = text_without_chars.Split(' ');

            LastText = text.ToLower();

            // Для Hex цвета
            var bc = new BrushConverter();

            if (IsUserMessage)
            {
                Dispatcher.Invoke(DispatcherPriority.Background,
                  new Action(() => {
                    TextBlock message = new TextBlock();
                    message.Text = $"Пользователь: {text}";
                    message.HorizontalAlignment = HorizontalAlignment.Right;
                    message.Margin = new Thickness(5);
                    message.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FF424242");
                    message.FontSize = 18;
                    message.TextWrapping = TextWrapping.Wrap;
                    message.Foreground = new SolidColorBrush(Colors.White);

                    panel.Children.Add(message);

                }));

                if (LastCommand == null) GetCommandInput(text);
                else CommandWithAcception(text);
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Background,
                  new Action(() => {
                    TextBlock message = new TextBlock();
                    message.Text = $"Хуба: {text}";
                    message.HorizontalAlignment = HorizontalAlignment.Left;
                    message.Margin = new Thickness(5);
                    message.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FF424242");
                    message.FontSize = 18;
                    message.TextWrapping = TextWrapping.Wrap;
                    message.Foreground = new SolidColorBrush(Colors.White);

                    panel.Children.Add(message);

                }));
            }
        }

        void CommandWithAcception(string acception)
        {
            if (LastCommand == "открыть ПО")
            {
                try
                {
                    int parsed = int.Parse(acception);

                    if (programs.ContainsKey(parsed))
                    {
                        AddNewMessage(false, $"Запускаю...");

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = programs[parsed],
                            UseShellExecute = true
                        });

                        programs.Clear();
                    }
                }

                catch
                {
                    AddNewMessage(false, "Отменяю...");
                }
            }

            if (acception == "да")
            {
                if (LastCommand == "выключить пк")
                {
                    Process.Start("shutdown", "/s /t 0");
                }

                if (LastCommand == "перезагрузка")
                {
                    Process.Start("shutdown", "-f -r");
                }
            }

            LastCommand = null;
        }

        async void GetCommandInput(string text)
        {
            DunaAI.ModelOutput result;

            var sampleData = new DunaAI.ModelInput()
            {
                Col0 = text,
            };

            //Load model and predict output
            await Task.Run(() =>
            {
                result = DunaAI.Predict(sampleData);

                if (result.Score[0] < 0.9274609) Response(result.Prediction);
                else AddNewMessage(false, "Сэр, перефразируйте пожалуйста, я не понял");
            });
        }

        async Task<string[]> GetArgsFromText(string text, double mainWordScore, double argsScore)
        {
            string[] chunks = text.Split(" ");
            string mainWord = "";
            List<string> arg = new List<string>();

            await Task.Run(() =>
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    Console.WriteLine(string.Join(" ", chunks[i]));
                    var sampleData = new DunaAI.ModelInput()
                    {
                        Col0 = chunks[i]
                    };

                    var result = DunaAI.Predict(sampleData);

                    Debug.WriteLine($"Внимание, переменная isCheckMainWord равна {isCheckMainWord}");

                    if (isCheckMainWord)
                    {
                        if (result.Score[0] < mainWordScore)
                        {
                            mainWord = chunks[i];
                        }

                        if (result.Score[0] > argsScore && Array.FindIndex(chunks, row => row.Contains(mainWord)) < Array.FindIndex(chunks, row => row.Contains(chunks[i])))
                        {
                            arg.Add(chunks[i]);
                        }
                    }

                    else
                    {
                        if (result.Score[0] > argsScore)
                        {
                            arg.Add(chunks[i]);
                        }
                    }

                    if (showArgsInfo) AddNewMessage(false, $"слово <{chunks[i]}>, цена <{result.Score[0]}>, главное слово <{mainWord}>");
                }
            });

            if (showArgsInfo) AddNewMessage(false, $"аргументы: <{string.Join($",", arg)}>");

            return arg.ToArray();
        }

        void Response(string response)
        {
            Dictionary<string, Action> commands = new Dictionary<string, Action> // список для команд
            { 
                { "погода", Weather },
                { "команды", Help },
                { "анекдот", Anekdot },
                { "время", TimeNow },
                { "посчитать", Calculate },
                { "перезагрузка", Reboot },
                { "выключить пк", Shutdown },
                { "ютуб", Youtube },
                { "википедия", Wikipedia },
                { "поддержка", Support },
                { "новости", News },
                { "настройки", SettingsMenu },
                { "скриншот", Screenshot },
                { "открыть ПО", OpenProgram },
                { "интернет", YandexSearch }
            };

            Dictionary<string, string> replies = new Dictionary<string, string> // список для ответов
            {
                { "приветствие", "Здраствуйте, сэр" },
                { "прощание", "Удачи, сэр" },
                { "осознание", "Сэр, я Хуба" },
                { "я", "Сэр, я хуба" },
                { "капец", "вообще жёстко, согласен" },
                { "удивление", "Сэр, я сам удивлён" },
                { "согласие", "да" },
                { "настроение", "Всё хорошо, сэр" },
                { "похвала", "Благодарю вас, сэр" },
                { "огорчение", "Сэр, я делаю что то не так?" },
                { "благодарность", "Я всегда к вашим услугам, сэр" },
                { "а ты знаешь", "Сэр, я бот, я незнаю о чём вы" },
                { "молодец", "Отлично, сэр" },
            };

            foreach (var command in commands)
            {
                if (command.Key == response)
                {
                    commands[response]();
                }
            }

            foreach (var reply in replies)
            {
                if (reply.Key == response)
                {
                    AddNewMessage(false, replies[response]);
                }
            }
        }

        string GetFirstWordForm(string word)
        {
            File.WriteAllText("input.txt", word);

            Process mystem = new Process();
            mystem.StartInfo.FileName = "mystem.exe";
            mystem.StartInfo.Arguments = "-n input.txt output.txt";
            mystem.StartInfo.UseShellExecute = true;
            mystem.StartInfo.CreateNoWindow = false;
            mystem.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mystem.Start();

            Thread.Sleep(500);

            string[] strs = File.ReadAllText("output.txt").Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
            mystem.Close();
            return strs[1];
        }

        // Сами функции

        void Reboot() 
        {
            AddNewMessage(false, "Вы уверены что хотите перезагрузить пк? (да, нет)");
            LastCommand = "перезагрузка";
        }

        void Shutdown()
        {
            AddNewMessage(false, "Вы уверены что хотите выключить пк? (да, нет)");
            LastCommand = "выключить пк";
        }

        void Help() { AddNewMessage(false, @"В данный момент я могу подсказать погоду и время, открыть видео на ютуб. Меня можно настроить написав мне <настройки>. Имею систему сохранений ваших настроек в папке AppData\Duna. Так же я могу подсказать IT новости"); }

        void Support()
        {
            string[] support_array = {
                "Сэр, не расстраивайтесь. Вы живёте спокойной жизнью, вы не бедны. У вас есть семья которая может поддержать, и я, который утешит вас :)", 
                "Сэр, я конечно бот, но всё таки не стоит расстраиваться. Возможно ваши проблемы не такие уш и большие. Разбейте её на маленькие части, и подумайте о них всех. Это поможет! :D", 
                "Я не совсем психолог, но вам стоит отвлечься. Займитесь любимым или полезным делом.", 
                "Возможно гусь в телеграмме может вам помочь. Поговорите с ним. Его id - @Ezh2testbot",
                "Грусть, это временное явление. Вам лучше сконцентрироваться не на ней, а на семье или важных деталях. Выполните какую нибудь задачу, что бы удовлетворить себя, или просто отдохните. Можете попробовать высказать всё кому то из семьи."
            };

            Random rand = new Random();
            AddNewMessage(false, support_array[rand.Next(0, support_array.Length)]);
        }

        void SettingsMenu() 
        {
            AddNewMessage(false, "Открываю настройки");
            Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() =>
            {
                Settings settings = new Settings();
                settings.Show();
            }));
        }

        async void News()
        {
            var config = Configuration.Default.WithDefaultLoader();
            string address = "https://habr.com/ru/news/";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var cellSelector = "a.tm-article-snippet__title-link";
            var cells = document.QuerySelectorAll(cellSelector);

            var title = cells.Select(m => m.TextContent);

            List<string> titles = title.ToList();

            title = null;

            for (int i = 0; i < 5; i++)
            {
                AddNewMessage(false, $"{titles[i]}");
            }

            title = null; titles = null;
            GC.Collect();
        }

        async void Weather()
        {
            string weatherText = LastText;
            string[] args = await GetArgsFromText(weatherText, 0.007, 0.03);

            foreach (string city_arg in args)
            {
                try
                {
                    string city = GetFirstWordForm(city_arg);
                    if (city == "город") city = null;

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("http://api.openweathermap.org");
                    var response = await client.GetAsync($"/data/2.5/weather?q={city}&appid=c44d8aa0c5e588db11ac6191c0bc6a60&units=metrics&lang=ru");

                    var stringResult = await response.Content.ReadAsStringAsync();

                    var obj = JsonConvert.DeserializeObject<dynamic>(stringResult);
                    double tmpDegrees = Math.Round(((float)obj.main.temp - 273.15), 2);
                    string description = obj.weather[0].description;
                    string name = obj.name;

                    AddNewMessage(false, string.Format("В городе {0} сейчас {1}, сэр", name, description));
                    AddNewMessage(false, string.Format("Температура около {0}°C", tmpDegrees));
                    GC.Collect();
                }
                catch (Exception e) { };
            }
        }

        async void Calculate()
        {
            string calculate_text = LastText;
            string result = "";
            calculate_text = calculate_text.Replace(" ", "");

            foreach (char letter in calculate_text)
            {
                if (Char.IsDigit(letter) || letter == '+' || letter == '-' || letter == '*' || letter == '/' || letter == '(' || letter == ')' || letter == '%')
                {
                    result = string.Concat(result, letter);
                }
            }

            try
            {
                AddNewMessage(false, $"Результат: {await Task.Run(() => Calculator.Calc(result))}");
            }

            catch (SyntaxErrorException)
            {
                AddNewMessage(false, "Вы неправильно ввели ваш пример, попробуйте ещё раз");
            }
        }

        void TimeNow()
        {
            AddNewMessage(false, $"Сейчас время: {DateTime.Now.ToLongTimeString()}");
            AddNewMessage(false, $"Сегодняшнее число же: {DateTime.Now.ToString("dd MMMM yyyy")}");
        }

        void Screenshot()
        {
            Application.Current.MainWindow.Hide();
            Thread.Sleep(milisecondForScreenshotDelay);

            Random rand = new Random();
            string file = null;

            for (int i = 0; i < int.MaxValue; i++)
            {
                if (!File.Exists($"Screenshots\\filename{i}.png"))
                {
                    file = $"Screenshots\\screen{i}.png";
                    break;
                }
            }
           
            using var bitmap = new Bitmap(1920, 1080);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(0, 0, 0, 0,
                bitmap.Size, CopyPixelOperation.SourceCopy);
            }

            bitmap.Save(file, ImageFormat.Png);

            Application.Current.MainWindow.Show();

            if (file != null) AddNewMessage(false, "Скриншот успешно сделан");
            else AddNewMessage(false, "Не удалось создать скриншот");
        }

        async void Anekdot()
        {
            var config = Configuration.Default.WithDefaultLoader();
            string address = "https://www.anekdot.ru/random/anekdot/";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var cellSelector = "div.text";
            var cells = document.QuerySelectorAll(cellSelector);
            var titles = cells.Select(m => m.TextContent);

            AddNewMessage(false, titles.First());
            titles = null;
        }
            
        async void Wikipedia() 
        {
            string[] args = await GetArgsFromText(LastText, 0.007, 0.03);

            // удаление <<про>> из аргументов как отдельное слово
            string charToRemove = "про";
            args = args.Where(val => val != charToRemove).ToArray();

            // удаление <<о>> в начале
            try
            {
                if (args[0] == "о")
                {
                    args = args.Skip(1).ToArray();
                }
            }

            catch { }

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = (await httpClient.GetAsync($"https://ru.wikipedia.org/w/api.php?format=xml&generator=search&action=query&prop=extracts&gsrsearch={string.Join(" ", args)}&redirects=true&gsrlimit=1")).EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseBody);

            var fnode = xmlDocument.GetElementsByTagName("extract")[0];

            try
            {
                string temp = fnode.InnerText;
                Regex reg = new Regex("\\<[^\\>]*\\>");
                temp = reg.Replace(temp, string.Empty);

                string[] array = temp.Split(".");

                AddNewMessage(false, array[0] + " " + array[1]);
            }

            catch (NullReferenceException)
            {
                AddNewMessage(false, $"Простите сэр, я не смог найти то что вам нужно. Попробуйте перефразировать или в настройках включите проверку главного слова");
            }

            catch (Exception e)
            {
                AddNewMessage(false, $"Сэр, произошла какая то ошибка: {e}");
                LogSystem.WriteToLogs(e.ToString());
            }
        }

        async void OpenProgram()
        {
            programs = ProgramSystem.GetProgramsFromText(LastText);

            foreach (var temp in programs)
            {
                Debug.WriteLine($"{temp.Key}, {temp.Value}");
            }

            if (programs.Count >= 2)
            {
                AddNewMessage(false, "Выберите программу, которую хотите запустить, отправив нужную цифру:");

                foreach (var temp in programs)
                {
                    FileInfo fileInfo = new FileInfo(temp.Value);

                    AddNewMessage(false, $"{temp.Key}. {fileInfo.Name}");
                }

                LastCommand = "открыть ПО";
            }
            
            else
            {
                foreach (var temp in programs)
                {
                    FileInfo fileInfo = new FileInfo(temp.Value);

                    AddNewMessage(false, $"Запускаю {fileInfo.Name}...");

                    await Task.Run(() =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = temp.Value,
                            UseShellExecute = true
                        });

                    });

                    programs.Clear();
                }
            }
        }

        async void Youtube() 
        {
            string[] args = await GetArgsFromText(LastText, 0.007, 0.018);

            AddNewMessage(false, "Открываю ютуб с набранными вами аргументами. Если я понял что то не так, прошу переформулируйте фразу");

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://www.youtube.com/results?search_query={string.Join(" ", args)}",
                UseShellExecute = true
            });

        }

        async void YandexSearch()
        {
            string[] args = await GetArgsFromText(LastText, 0.007, 0.019);

            AddNewMessage(false, "Открываю поисковик...");

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://yandex.ru/search/?text={string.Join(" ", args)}",
                UseShellExecute = true
            });

        }
    }
}

// Класс для калькулятора (хз почему выделил в класс)
public static class Calculator
{
    private static DataTable Table { get; } = new DataTable();
    public static double Calc(string Expression) => Convert.ToDouble(Table.Compute(Expression, string.Empty));
}