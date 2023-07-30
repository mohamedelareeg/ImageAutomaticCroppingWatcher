using Components.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ImageAutomaticCroppingWatcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the DbContext here
            using (var context = new LogEntryContext())
            {
                context.Database.EnsureCreated();
            }


            LoadSettings();

        }
        private void LoadSettings()
        {

            var defaultSettings = new SettingsModel();
            SharedSettings.Instance.SetSettings(defaultSettings);
        }
    }
}
