using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Components.Utils
{
        public class SharedSettings
        {
            private static SharedSettings instance;
            private SettingsModel loadedSettings;

            private SharedSettings()
            {
            }

            public static SharedSettings Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new SharedSettings();
                    }
                    return instance;
                }
            }

            public SettingsModel LoadedSettings
            {
                get { return loadedSettings; }
            }

            public void SetSettings(SettingsModel settings)
            {
                loadedSettings = settings;
            }

            public SettingsModel GetSettings()
            {
                return loadedSettings;
            }
        }


    public class SettingsModel
    {
        public SettingsModel()
        {
        }
        public SettingsModel(int _templateId)
        {
            this.TemplateId = _templateId;
        }
        public int TemplateId { get; set; }
        public int Version { get; set; } = 1;

        //Image Settings
        public string FileFormat { get; set; } = ".pdf";
        public double? ImageCompression { get; set; } = 100;
        public double? ImageRotation { get; set; } = 0;
        public string OutputFileNaming { get; set; }

        public string WatcherPath { get; set; } = "C:\\BatchesPro";
        public bool ActiveSharedFolderWatcher { get; set; } = false;
        public bool ActiveSharedFolderApi { get; set; } = false;
        public string WatcherSharedFolder { get; set; } = "\\10.10.10.190";
        public string WatcherReleasePath { get; set; } = "C:\\releasePdf";
        public string WatcherReleaseApi { get; set; } = "https://localhost:7004/";
        public string WatcherReleaseApiFolder { get; set; } = "Scan";
        public string WatcherLogFolder { get; set; } = "C:\\NICapturePro\\logs";
   

    }

}
