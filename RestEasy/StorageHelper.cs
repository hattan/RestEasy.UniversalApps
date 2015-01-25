using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage;

namespace RestEasy
{
    /*Fork of the StorageHelper class created by Jerry Nixon. I've made a few small modifications
     * including using Json.net to serialize/deserialize data
     * http://blog.jerrynixon.com/2012/06/windows-8-how-to-read-files-in-winrt.html
     * */
    public static class StorageHelper
    {
        #region Settings

        public static bool SettingExists(string key, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    return ApplicationData.Current.LocalSettings.Values.ContainsKey(key);
                case StorageStrategies.Roaming:
                    return ApplicationData.Current.RoamingSettings.Values.ContainsKey(key);
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        public static T GetSetting<T>(string key, T otherwise = default(T),
                                      StorageStrategies location = StorageStrategies.Local)
        {
            try
            {
                if (!(SettingExists(key, location)))
                    return otherwise;

                switch (location)
                {
                    case StorageStrategies.Local:
                        return (T)ApplicationData.Current.LocalSettings.Values[key];
                    case StorageStrategies.Roaming:
                        return (T)ApplicationData.Current.RoamingSettings.Values[key];
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch
            {
                return otherwise;
            }
        }

        public static void SetSetting<T>(string key, T value, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    ApplicationData.Current.LocalSettings.Values[key] = value;
                    break;
                case StorageStrategies.Roaming:
                    ApplicationData.Current.RoamingSettings.Values[key] = value;
                    break;
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        public static void DeleteSetting(string key, StorageStrategies location = StorageStrategies.Local)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    ApplicationData.Current.LocalSettings.Values.Remove(key);
                    break;
                case StorageStrategies.Roaming:
                    ApplicationData.Current.RoamingSettings.Values.Remove(key);
                    break;
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        #endregion

        #region File

        public static async Task<bool> FileExistsAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            return (await GetIfFileExistsAsync(key, location)) != null;
        }

        public static async Task<bool> FileExistsAsync(string key, StorageFolder folder)
        {
            return (await GetIfFileExistsAsync(key, folder)) != null;
        }

        public static async Task<bool> DeleteFileAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            StorageFile file = await GetIfFileExistsAsync(key, location);
            if (file != null)
                await file.DeleteAsync();
            return !(await FileExistsAsync(key, location));
        }

        public static async Task<string> ReadFileAsync(string key, StorageStrategies location = StorageStrategies.Local)
        {
            // fetch file
            StorageFile file = await GetIfFileExistsAsync(key, location);

            // read content
            string _String = await FileIO.ReadTextAsync(file);

            return _String;
        }

        public static async Task<T> ReadFileAsync<T>(string key, StorageStrategies location = StorageStrategies.Local)
        {
            // fetch file
            StorageFile file = await GetIfFileExistsAsync(key, location);
            if (file == null)
                return default(T);
            // read content
            string _String = await FileIO.ReadTextAsync(file);
            // convert to obj
            var result = Deserialize<T>(_String);
            return result;
        }

        public static async Task<bool> WriteFileAsync(string key, string body,
                                                      StorageStrategies location = StorageStrategies.Local)
        {
            // create file
            StorageFile file = await CreateFileAsync(key, location, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, body);
            // result
            return await FileExistsAsync(key, location);
        }

        public static async Task<bool> WriteFileAsync<T>(string key, T value,
                                                         StorageStrategies location = StorageStrategies.Local)
        {
            // create file
            StorageFile file = await CreateFileAsync(key, location, CreationCollisionOption.ReplaceExisting);
            // convert to string
            string _String = Serialize(value);
            // save string to file
            await FileIO.WriteTextAsync(file, _String);
            // result
            return await FileExistsAsync(key, location);
        }

        private static async Task<StorageFile> CreateFileAsync(string key,
                                                               StorageStrategies location = StorageStrategies.Local,
                                                               CreationCollisionOption option =
                                                                   CreationCollisionOption.OpenIfExists)
        {
            switch (location)
            {
                case StorageStrategies.Local:
                    return await ApplicationData.Current.LocalFolder.CreateFileAsync(key, option);
                case StorageStrategies.Roaming:
                    return await ApplicationData.Current.RoamingFolder.CreateFileAsync(key, option);
                case StorageStrategies.Temporary:
                    return await ApplicationData.Current.TemporaryFolder.CreateFileAsync(key, option);
                default:
                    throw new NotSupportedException(location.ToString());
            }
        }

        private static async Task<StorageFile> GetIfFileExistsAsync(string key, StorageFolder folder)
        {
            StorageFile retval;
            try
            {
                retval = await folder.GetFileAsync(key);
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("GetIfFileExistsAsync:FileNotFoundException");
                return null;
            }
            return retval;
        }


        private static async Task<StorageFile> GetIfFileExistsAsync(string key,
                                                                    StorageStrategies location = StorageStrategies.Local)
        {
            StorageFile retval;
            try
            {
                switch (location)
                {
                    case StorageStrategies.Local:
                        retval = await ApplicationData.Current.LocalFolder.GetFileAsync(key);
                        break;
                    case StorageStrategies.Roaming:
                        retval = await ApplicationData.Current.RoamingFolder.GetFileAsync(key);
                        break;
                    case StorageStrategies.Temporary:
                        retval = await ApplicationData.Current.TemporaryFolder.GetFileAsync(key);
                        break;
                    default:
                        throw new NotSupportedException(location.ToString());
                }
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("GetIfFileExistsAsync:FileNotFoundException");
                return null;
            }

            return retval;
        }

        #endregion

        public enum StorageStrategies
        {
            Local,
            Roaming,
            Temporary
        }

        private static string Serialize(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        private static T Deserialize<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static async void DeleteFileFireAndForget(string key, StorageStrategies location)
        {
            await DeleteFileAsync(key, location);
        }

        public static async void WriteFileFireAndForget(string key, string body, StorageStrategies location)
        {
            await WriteFileAsync(key, body, location);
        }

        public static async void WriteFileFireAndForget<T>(string key, T value, StorageStrategies location)
        {
            await WriteFileAsync(key, value, location);
        }
    }
}