﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Duna.SaveModules
{
    internal class SaveSystem
    {
        /// <summary>
        /// Класс, который умеет сохранять данные и загружать.
        /// </summary>
        public class SaveRepository : IDisposable
        {
            private readonly string _rootDirectory;
            private readonly string _fileName;
            private readonly string _pathToFile;
            private readonly SemaphoreSlim _semaphore;

            public SaveRepository(string rootDirectory, string fileName)
            {
                _rootDirectory = rootDirectory;
                _fileName = fileName;
                _pathToFile = Path.Combine(_rootDirectory, fileName);
                _semaphore = new SemaphoreSlim(1);
            }

            public async Task<SaveData> Load()
            {
                await _semaphore.WaitAsync();
                var data = new SaveData();
                try
                {
                    await Task.Run(() =>
                    {
                        if (File.Exists(_pathToFile))
                        {
                            using var stream = File.OpenRead(_pathToFile);
                            using var reader = new BinaryReader(stream);
                            data.checkMainWord = reader.ReadBoolean();
                            data.showArgsInfo = reader.ReadBoolean();
                        }
                    });
                    return data;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public async Task Save(SaveData data)
            {
                await _semaphore.WaitAsync();
                try
                {
                    await Task.Run(() =>
                    {
                        if (!Directory.Exists(_rootDirectory))
                        {
                            Directory.CreateDirectory(_rootDirectory);
                        }

                        using var stream = File.OpenWrite(_pathToFile);
                        using var writer = new BinaryWriter(stream);
                        writer.Write(data.checkMainWord);
                        writer.Write(data.showArgsInfo);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            public void Dispose()
            {
                _semaphore?.Dispose();
            }
        }
    }

    public class SaveData
    {
        /// <summary>
        /// </summary>
        public bool checkMainWord { get; set; }
        public bool showArgsInfo { get; set; }
    }
}
