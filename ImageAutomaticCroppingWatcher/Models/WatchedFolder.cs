
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageAutomaticCroppingWatcher.Models
{
    public class WatchedFolder : INotifyPropertyChanged
    {
        private int id;
        private string folderPath;
        private int fileCount;
        private int successfulUploaded;
        private DateTime lastWatched;

        public int Id
        {
            get { return id; }
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public string FolderPath
        {
            get { return folderPath; }
            set
            {
                if (folderPath != value)
                {
                    folderPath = value;
                    OnPropertyChanged(nameof(FolderPath));
                }
            }
        }

        public int FileCount
        {
            get { return fileCount; }
            set
            {
                if (fileCount != value)
                {
                    fileCount = value;
                    SaveFilesToJson();
                    OnPropertyChanged(nameof(FileCount));
                }
            }
        }
        public int SuccessfulUploaded
        {
            get { return successfulUploaded; }
            set
            {
                if (successfulUploaded != value)
                {
                    successfulUploaded = value;
                    OnPropertyChanged(nameof(SuccessfulUploaded));
                }
            }
        }

        public DateTime LastWatched
        {
            get { return lastWatched; }
            set
            {
                if (lastWatched != value)
                {
                    lastWatched = value;
                    OnPropertyChanged(nameof(LastWatched));
                }
            }
        }
        public void SaveFilesToJson()
        {
            try
            {

           
            var files = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);

            // Serialize the list of files to JSON
            string json = JsonConvert.SerializeObject(files, Formatting.Indented);

            // Save the JSON file to the folder path
            string jsonFilePath = Path.Combine(FolderPath, "files.json");

            // Write to the file using a FileStream with FileShare.Read option
            using (var fileStream = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(json);
                }
            }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }


        public void SaveFilesToJson(string filePath)
        {
            var files = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);

            // Serialize the list of files to JSON
            string json = JsonConvert.SerializeObject(files, Formatting.Indented);

            // Save the JSON file to the folder path
            string jsonFilePath = Path.Combine(FolderPath, "files.json");
            File.WriteAllText(jsonFilePath, json);

            // Add the current image path to the list of successfully uploaded files
            List<string> successfulUploadedFiles = new List<string>();

            // Deserialize the JSON file if it exists
            if (File.Exists(jsonFilePath))
            {
                string jsonContent;
                using (var fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(fileStream))
                {
                    jsonContent = reader.ReadToEnd();
                }
                successfulUploadedFiles = JsonConvert.DeserializeObject<List<string>>(jsonContent);
            }

            // Add the current image path to the list
            successfulUploadedFiles.Add(filePath);

            // Serialize the updated list of files to JSON
            string updatedJson = JsonConvert.SerializeObject(successfulUploadedFiles, Formatting.Indented);

            // Save the updated JSON file
            File.WriteAllText(jsonFilePath, updatedJson);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
