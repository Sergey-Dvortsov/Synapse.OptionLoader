namespace Synapse.OptionLoader
{
    using System;
    using System.IO;
    using Synapse.Common;

    /// <summary>
    /// Создает структуру каталогов
    /// </summary>
    public class FolderBuilder : IFolderBuilder
    {

        /// <summary>
        /// Возвращает путь к корневой папке
        /// </summary>
        /// <returns></returns>
        public string GetAppFolder()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, GetAppName());
       }

        /// <summary>
        /// Возвращает путь к папке логов
        /// </summary>
        /// <returns></returns>
        public string GetLogFolder()
        {
            var appFolder = GetAppFolder();
            var logFolder = Path.Combine(appFolder, "Logs");
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            return logFolder;
        }

        /// <summary>
        /// Возвращает путь к папке настроек SmartCom
        /// </summary>
        /// <returns></returns>
        public string GetSmartFolder()
        {
            var appFolder = GetAppFolder();
            var smartFolder = Path.Combine(appFolder, "Smart");
            if (!Directory.Exists(smartFolder))
                Directory.CreateDirectory(smartFolder);
            return smartFolder;
        }

        /// <summary>
        /// Возвращает путь к папке настроек Quik
        /// </summary>
        /// <returns></returns>
        public string GetQuikFolder()
        {
            var appFolder = GetAppFolder();
            var quikFolder = Path.Combine(appFolder, "Quik");
            if (!Directory.Exists(quikFolder))
                Directory.CreateDirectory(quikFolder);
            return quikFolder;
        }

        /// <summary>
        /// Возвращает путь к папке хранилища
        /// </summary>
        /// <returns></returns>
        public string GetStorageFolder()
        {
            var appFolder = GetAppFolder();
            var storageFolder = Path.Combine(appFolder, "History");

            if (!Directory.Exists(storageFolder))
                Directory.CreateDirectory(storageFolder);

            return storageFolder;
        }

        /// <summary>
        /// Возвращает имя приложения
        /// </summary>
        /// <returns></returns>
        public string GetAppName()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        }


    }
}
