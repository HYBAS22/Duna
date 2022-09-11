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
using System.Text.RegularExpressions;

namespace Duna
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string LastText;
        public static bool isCheckMainWord = false;
        public static bool showArgsInfo = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSavedData();
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

            data = await repository.Load();
            isCheckMainWord = data.checkMainWord;
            showArgsInfo = data.showArgsInfo;
            Debug.WriteLine($"вывод после загрузки: {data.checkMainWord}, {data.showArgsInfo}");
        }

        void AddNewMessage(bool IsUserMessage, string text)
        {
            // Обработка текста, удаление знаков и т.д
            text = text.ToLower();
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

            LastText = text;

            // Для Hex цвета
            var bc = new BrushConverter();

            if (IsUserMessage)
            {
                TextBlock message = new TextBlock();
                message.Text = $"Пользователь: {text}";
                message.HorizontalAlignment = HorizontalAlignment.Right;
                message.Margin = new Thickness(5);
                message.Background = (Brush)bc.ConvertFrom("#FF424242");
                message.FontSize = 18;
                message.TextWrapping = TextWrapping.Wrap;
                message.Foreground = new SolidColorBrush(Colors.White);

                panel.Children.Add(message);

                GetCommandInput(text);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TextBlock message = new TextBlock();
                    message.Text = $"Хуба: {text}";
                    message.HorizontalAlignment = HorizontalAlignment.Left;
                    message.Margin = new Thickness(5);
                    message.Background = (Brush)bc.ConvertFrom("#FF424242");
                    message.FontSize = 18;
                    message.TextWrapping = TextWrapping.Wrap;
                    message.Foreground = new SolidColorBrush(Colors.White);

                    panel.Children.Add(message);

                });
            }
        }

        void GetCommandInput(string text)
        {
            DunaAI.ModelOutput result;

            var sampleData = new DunaAI.ModelInput()
            {
                Col0 = text,
            };

            //Load model and predict output
            result = DunaAI.Predict(sampleData);

            if (result.Score[0] < 0.9274609) Response(result.Prediction);
            else AddNewMessage(false, "Сэр, перефразируйте пожалуйста, я не понял");
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
                { "ютуб", Youtube },
                { "википедия", Wikipedia },
                { "поддержка", Support },
                { "новости", News },
                { "настройки", SettingsMenu }
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
            AddNewMessage(false, "Простите, ребут ещё не доступен"); 
        }

        void Help() { AddNewMessage(false, @"В данный момент я могу подсказать погоду и время, открыть видео на ютуб. Меня можно настроить написав мне <настройки>. Имею систему сохранений ваших настроек в папке AppData\Duna"); }

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
            Settings settings = new Settings();
            settings.Show();
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
            string[] args = await GetArgsFromText(weatherText, 0.007, 0.033);

            foreach (string city_arg in args)
            {
                try
                {
                    string temp = GetFirstWordForm(city_arg);

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri("http://api.openweathermap.org");
                    var response = await client.GetAsync($"/data/2.5/weather?q={temp}&appid=c44d8aa0c5e588db11ac6191c0bc6a60&units=metrics&lang=ru");

                    // This line gives me error
                    var stringResult = await response.Content.ReadAsStringAsync();

                    var obj = JsonConvert.DeserializeObject<dynamic>(stringResult);
                    double tmpDegrees = Math.Round(((float)obj.main.temp - 273.15), 2);
                    string name = obj.name;

                    AddNewMessage(false, string.Format("В городе {0} сейчас {1}°C сэр", name, tmpDegrees));
                    GC.Collect();
                }
                catch (Exception) { };
            }
        }

        void Calculate()
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

            AddNewMessage(false, $"Результат: {Calculator.Calc(result)}");
        }

        void TimeNow()
        {
            AddNewMessage(false, $"Сейчас время: {DateTime.Now.ToLongTimeString()}");
            AddNewMessage(false, $"Сегодняшнее число же: {DateTime.Now.ToString("dd MMMM yyyy")}");
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
            string[] args = await GetArgsFromText(LastText, 0.007, 0.04);

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
            }
        }

        async void Youtube() 
        {
            string[] args = await GetArgsFromText(LastText, 0.007, 0.04);

            AddNewMessage(false, "Открываю ютуб с набранными вами аргументами. Если я понял что то не так, прошу переформулируйте фразу");

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://www.youtube.com/results?search_query={string.Join(" ", args)}",
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