using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FluencySDK
{
    public class FileStorageAdapter : IStorageAdapter
    {
        private readonly string _storagePath;

        // Constructor that takes a specific file path
        public FileStorageAdapter(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }
            // Ensure the directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _storagePath = filePath;
        }

        // Constructor that creates a path in the user's local app data
        public FileStorageAdapter(string appName, string fileName = "fluencyState.json")
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("App name cannot be null or whitespace.", nameof(appName));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
            }

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storagePath = Path.Combine(localAppDataPath, appName, fileName);
            
            string directory = Path.GetDirectoryName(_storagePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<T> GetItemAsync<T>(string key) // key is effectively the filename part of _storagePath
        {
            // In this simple adapter, the key isn't used to differentiate files, 
            // as the _storagePath already points to a specific file.
            // If multiple states were needed with one adapter, key would determine filename based on this key.
            string actualPath = Path.Combine(Path.GetDirectoryName(_storagePath), key + Path.GetExtension(_storagePath));
            if(!_storagePath.EndsWith(key + Path.GetExtension(_storagePath))) actualPath = _storagePath; // Fallback for single-file constructor

            if (File.Exists(actualPath))
            {
                string json = await ReadTextAsync(actualPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            return default(T); // Return default if file doesn't exist or is empty
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            string actualPath = Path.Combine(Path.GetDirectoryName(_storagePath), key + Path.GetExtension(_storagePath));
             if(!_storagePath.EndsWith(key + Path.GetExtension(_storagePath))) actualPath = _storagePath; // Fallback for single-file constructor

            string json = JsonConvert.SerializeObject(value, Formatting.Indented);
            await WriteTextAsync(actualPath, json);
        }

        public Task RemoveItemAsync(string key)
        {
            string actualPath = Path.Combine(Path.GetDirectoryName(_storagePath), key + Path.GetExtension(_storagePath));
            if(!_storagePath.EndsWith(key + Path.GetExtension(_storagePath))) actualPath = _storagePath; // Fallback for single-file constructor

            if (File.Exists(actualPath))
            {
                File.Delete(actualPath);
            }
            return Task.CompletedTask;
        }

        // Using async file operations for better performance in UI/server apps, though simple for this context.
        private async Task<string> ReadTextAsync(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = System.Text.Encoding.UTF8.GetBytes(text);
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await stream.WriteAsync(encodedText, 0, encodedText.Length);
            }
        }
    }
} 