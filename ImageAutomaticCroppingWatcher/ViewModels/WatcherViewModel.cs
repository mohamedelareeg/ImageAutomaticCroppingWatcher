using Components.Utils;
using ImageAutomaticCroppingWatcher.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace ImageAutomaticCroppingWatcher.ViewModels
{
    public class WatcherViewModel : INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient;
        public readonly string targetFolder = SharedSettings.Instance.LoadedSettings.WatcherPath;
        public readonly string apiBaseUrl = SharedSettings.Instance.LoadedSettings.WatcherReleaseApi;
        public readonly string ImagesExtention = "*" + SharedSettings.Instance.LoadedSettings.FileFormat;
        public readonly string _ImagesExtention = SharedSettings.Instance.LoadedSettings.FileFormat;
        private readonly string container = SharedSettings.Instance.LoadedSettings.WatcherReleaseApiFolder;
        private WatchedFolder selectedWatchedFolder;
        private ObservableCollection<LogEntry> logEntries;
        private ObservableCollection<WatchedFolder> watchedFolders;
        private LogEntryContext context;
        private readonly Dispatcher dispatcher;

        public WatcherViewModel()
        {
            _httpClient = new HttpClient();
            //InitializeLogFile();
            context = new LogEntryContext();
            dispatcher = Dispatcher.CurrentDispatcher;

            logEntries = new ObservableCollection<LogEntry>(context.LogEntries);
            watchedFolders = new ObservableCollection<WatchedFolder>(context.WatchedFolders);

            LoadLogEntries();
            LoadWatchedFolders();
            //CheckNewFolders();
            /*
            var timer = new Timer(60000);
            timer.Elapsed += async (sender, e) =>
            {
                if (CheckNetworkConnection())
                {
                    timer.Stop();
                    await RetryUploadFailedFiles();
                    timer.Start();
                }
            };
            timer.Start();
            */
        }
        private void LoadLogEntries()
        {
            dispatcher.Invoke(() =>
            {
                LogEntries.Clear();
                foreach (var logEntry in context.LogEntries)
                {
                    LogEntries.Add(logEntry);
                }
            });
        }

        private void LoadWatchedFolders()
        {
            dispatcher.Invoke(() =>
            {
                WatchedFolders.Clear();
                foreach (var watchedFolder in context.WatchedFolders)
                {
                    WatchedFolders.Add(watchedFolder);
                }
            });
        }

      
        public async void OnFileCreated(object sender, FileSystemEventArgs e)
        {

            var folders = Directory.GetDirectories(targetFolder, "*", SearchOption.TopDirectoryOnly);

            bool pathExistsInFolders = folders.Contains(e.FullPath);

            if (pathExistsInFolders)
            {
                using (var context = new LogEntryContext())
                {
                    var files = Directory.GetFiles(e.FullPath, ImagesExtention, SearchOption.AllDirectories);
                    WatchedFolder newWatchedFolder = new WatchedFolder { FolderPath = e.FullPath, LastWatched = DateTime.Now, FileCount = files.Length };
                    context.WatchedFolders.Add(newWatchedFolder);
                    dispatcher.Invoke(() =>
                    {
                        WatchedFolders.Add(newWatchedFolder);
                    });
               
                }

            }
            // Add the file path to the upload queue
            uploadQueue.Enqueue(e.FullPath);

            // Start the upload process if it's not already in progress
            if (!isUploading)
            {
                ProcessUploadQueue();
            }
            /*
            await Task.Delay(5000); // Add a delay of 5 seconds (adjust as needed)
            await UploadFile(e.FullPath);

            */

        }
        private bool isUploading = false;
        private Queue<string> uploadQueue = new Queue<string>();

        private async void ProcessUploadQueue()
        {
            // Check if upload is already in progress
            if (isUploading)
            {
                return;
            }

            // Set uploading flag to true
            isUploading = true;

            while (uploadQueue.Count > 0)
            {
                string filePath = uploadQueue.Dequeue();

              

                // Upload the file
                bool uploadStatus = await UploadFile(filePath);

                // Perform actions based on upload status
                if (uploadStatus)
                {
                   
                }
                else
                {

                }
            }

            // Set uploading flag to false
            isUploading = false;
        }

        public async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var folders = Directory.GetDirectories(targetFolder, "*", SearchOption.TopDirectoryOnly);

            bool pathExistsInFolders = folders.Contains(e.FullPath);

            if (pathExistsInFolders)
            {
                using (var context = new LogEntryContext())
                {
                    var folderPath = e.FullPath;
                    var watchedFolder = context.WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);

                    if (watchedFolder != null)
                    {
                        var files = Directory.GetFiles(e.FullPath, ImagesExtention, SearchOption.AllDirectories);
                        watchedFolder.FileCount = files.Length;
                        context.WatchedFolders.Update(watchedFolder); // Update the WatchedFolder entity in the context
                        context.SaveChanges();
                    }
                    else
                    {
                        watchedFolder = new WatchedFolder { FolderPath = folderPath, LastWatched = DateTime.Now };
                        context.WatchedFolders.Add(watchedFolder);
                        context.SaveChanges();
                    }

                    dispatcher.Invoke(() =>
                    {
                        // Update the corresponding item in the WatchedFolders collection
                        var existingWatchedFolder = WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);
                        if (existingWatchedFolder != null)
                        {
                            existingWatchedFolder.FileCount = watchedFolder.FileCount;
                        }
                        else
                        {
                            WatchedFolders.Add(watchedFolder);
                        }
                    });
                }



            }
            //await Task.Delay(5000); // Add a delay of 5 seconds (adjust as needed)
            //await UploadFile(e.FullPath);
        }
        public async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            var folders = Directory.GetDirectories(targetFolder, "*", SearchOption.TopDirectoryOnly);

            bool pathExistsInFolders = folders.Contains(e.FullPath);

            if (pathExistsInFolders)
            {
                using (var context = new LogEntryContext())
                {
                    var folderPath = e.FullPath;
                    var watchedFolder = context.WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);

                    if (watchedFolder != null)
                    {
                        var files = Directory.GetFiles(e.FullPath, ImagesExtention, SearchOption.AllDirectories);
                        watchedFolder.FileCount = files.Length;
                        context.WatchedFolders.Update(watchedFolder); // Update the WatchedFolder entity in the context
                        context.SaveChanges();
                    }
                    else
                    {
                        watchedFolder = new WatchedFolder { FolderPath = folderPath, LastWatched = DateTime.Now };
                        context.WatchedFolders.Add(watchedFolder);
                        context.SaveChanges();
                    }

                    dispatcher.Invoke(() =>
                    {
                        // Update the corresponding item in the WatchedFolders collection
                        var existingWatchedFolder = WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);
                        if (existingWatchedFolder != null)
                        {
                            existingWatchedFolder.FileCount = watchedFolder.FileCount;
                        }
                        else
                        {
                            WatchedFolders.Add(watchedFolder);
                        }
                    });
                }
            }
        }

        public async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            var folders = Directory.GetDirectories(targetFolder, "*", SearchOption.TopDirectoryOnly);

            bool pathExistsInFolders = folders.Contains(e.FullPath);

            if (pathExistsInFolders)
            {
                using (var context = new LogEntryContext())
                {
                    var folderPath = e.FullPath;
                    var watchedFolder = context.WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);

                    if (watchedFolder != null)
                    {
                        var files = Directory.GetFiles(e.FullPath, ImagesExtention, SearchOption.AllDirectories);
                        watchedFolder.FileCount = files.Length;
                        context.WatchedFolders.Update(watchedFolder); // Update the WatchedFolder entity in the context
                        context.SaveChanges();
                    }
                    else
                    {
                        watchedFolder = new WatchedFolder { FolderPath = folderPath, LastWatched = DateTime.Now };
                        context.WatchedFolders.Add(watchedFolder);
                        context.SaveChanges();
                    }

                    dispatcher.Invoke(() =>
                    {
                        // Update the corresponding item in the WatchedFolders collection
                        var existingWatchedFolder = WatchedFolders.FirstOrDefault(wf => wf.FolderPath == folderPath);
                        if (existingWatchedFolder != null)
                        {
                            existingWatchedFolder.FileCount = watchedFolder.FileCount;
                        }
                        else
                        {
                            WatchedFolders.Add(watchedFolder);
                        }
                    });
                }
            }
        }

        /*
        private void InitializeLogFile()
        {
            using (var context = new LogEntryContext())
            {
                context.Database.EnsureCreated();
            }
        }
        */


        private void CheckNewFolders()
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            var allFolders = Directory.GetDirectories(targetFolder);

            using (var context = new LogEntryContext())
            {
                var watchedFolders = context.WatchedFolders.ToList();

                foreach (var folderPath in allFolders)
                {
                    var files = Directory.GetFiles(folderPath, ImagesExtention, SearchOption.AllDirectories);
                    if (watchedFolders.Any(wf => wf.FolderPath == folderPath && wf.FileCount == files.Length))
                    {
                        continue;
                    }

                  
                    
                   
                    WatchedFolder newWatchedFolder = new WatchedFolder { FolderPath = folderPath, LastWatched = DateTime.Now, FileCount = files.Length };
                    context.WatchedFolders.Add(newWatchedFolder);
                    dispatcher.Invoke(() =>
                    {
                        WatchedFolders.Add(newWatchedFolder);
                    });
                    WatchFolder(folderPath, DateTime.Now);
                }

                context.SaveChanges();

            }
        }

        private async void WatchFolder(string folderPath, DateTime lastWatched)
        {
            var folders = Directory.GetDirectories(folderPath, ImagesExtention, SearchOption.AllDirectories);
            var files = Directory.GetFiles(folderPath, ImagesExtention, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                await UploadFile(file);
            }
        }

        public async Task<bool> UploadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return false;
            }

            bool status = await PDFHelper.AutoMaticCroppingPDFImages(filePath);

            return status;

        }


        private async Task UpdateLogEntryStatus(string filePath, string status)
        {
            using (var context = new LogEntryContext())
            {
                var logEntry = context.LogEntries.FirstOrDefault(e => e.FilePath == filePath);

                if (logEntry == null)
                {
                    logEntry = new LogEntry { FilePath = filePath, Status = status, Date = DateTime.Now };
                    context.LogEntries.Add(logEntry);
                    dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(logEntry);
                    });
                }
                else
                {
                    logEntry.Status = status;
                }

                await context.SaveChangesAsync();
            }
        }

        private async Task RetryUploadFailedFiles()
        {
            using (var context = new LogEntryContext())
            {
                var logEntries = context.LogEntries.OrderBy(entry => entry.Date).ToList();

                foreach (var entry in logEntries)
                {
                    bool status = await UploadFile(entry.FilePath);

                    if (status == true)
                    {
                        context.LogEntries.Remove(entry);
                        dispatcher.Invoke(() =>
                        {
                            LogEntries.Remove(entry);
                        });
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private bool CheckNetworkConnection()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = httpClient.GetAsync(apiBaseUrl).Result;
                    return result.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public ObservableCollection<LogEntry> LogEntries
        {
            get { return logEntries; }
            set
            {
                logEntries = value;
                OnPropertyChanged(nameof(LogEntries));
            }
        }

        public ObservableCollection<WatchedFolder> WatchedFolders
        {
            get { return watchedFolders; }
            set
            {
                watchedFolders = value;
                OnPropertyChanged(nameof(WatchedFolders));
            }
        }

        public WatchedFolder SelectedWatchedFolder
        {
            get { return selectedWatchedFolder; }
            set
            {
                selectedWatchedFolder = value;
                OnPropertyChanged(nameof(SelectedWatchedFolder));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
