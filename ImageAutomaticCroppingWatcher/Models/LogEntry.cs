using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageAutomaticCroppingWatcher.Models
{
    public class LogEntry : INotifyPropertyChanged
    {
        private int logEntryId;
        private string filePath;
        private string status;
        private DateTime date;

        public int LogEntryId
        {
            get { return logEntryId; }
            set
            {
                if (logEntryId != value)
                {
                    logEntryId = value;
                    OnPropertyChanged(nameof(LogEntryId));
                }
            }
        }

        public string FilePath
        {
            get { return filePath; }
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    OnPropertyChanged(nameof(FilePath));
                }
            }
        }

        public string Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public DateTime Date
        {
            get { return date; }
            set
            {
                if (date != value)
                {
                    date = value;
                    OnPropertyChanged(nameof(Date));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
