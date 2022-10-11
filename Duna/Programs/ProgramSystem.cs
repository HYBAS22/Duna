using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duna.Programs
{
    public static class ProgramSystem
    {
        static Dictionary<int, string> ProgramsDictionary = new Dictionary<int, string>();
        static string programsFile = "Programs\\programs.txt";

        static int i = 0;

        public static Dictionary<int, string> GetProgramsFromText(string text)
        {
            using (StreamReader fs = new StreamReader(programsFile))
            {
                while (true)
                {
                    string[] names, words;
                    string temp, path;

                    words = text.Split(" ");

                    // Читаем строку из файла во временную переменную.
                    temp = fs.ReadLine();

                    // Если достигнут конец файла, прерываем считывание.
                    if (temp == null) break;

                    path = temp.Split("|")[1]; // путь к файлу

                    string crossed_temp = temp.Split("|")[0]; // ищем названия файлов
                    names = crossed_temp.Split(","); // сами названия файлов

                    foreach (string name in names)
                    {
                        if (words.Contains(name))
                        {
                            if (!ProgramsDictionary.ContainsValue(path))
                            {
                                ProgramsDictionary.Add(i++, path);
                            }
                        }
                    }
                }
            }

            return ProgramsDictionary;
        }
    }
}
