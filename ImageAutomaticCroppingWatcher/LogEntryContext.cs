
using ImageAutomaticCroppingWatcher.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageAutomaticCroppingWatcher
{
    public class LogEntryContext : DbContext
    {
        public DbSet<LogEntry> LogEntries { get; set; }

        public DbSet<WatchedFolder> WatchedFolders { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Get the folder path
            string folderPath = @"C:\NICapturePro\settings";

            // Create the folder if it does not exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Set up the SQLite connection
            optionsBuilder.UseSqlite($"Data Source={folderPath}\\automatic_cropping_log.db"); ;
        }

    }
}
