using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Timers;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.ComponentModel;
using System.Collections.ObjectModel;
using ImageAutomaticCroppingWatcher.ViewModels;
using ImageAutomaticCroppingWatcher.Models;

namespace ImageAutomaticCroppingWatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
  
   
     
    public partial class WatcherWindow : Window
    {
        private FileSystemWatcher watcher;

        private WatcherViewModel viewModel;

      
        public WatcherWindow()
        {
            InitializeComponent();
            viewModel = new WatcherViewModel();
            DataContext = viewModel;


         

            watcher = new FileSystemWatcher(viewModel.targetFolder);
            watcher.IncludeSubdirectories = true;
            watcher.Created += viewModel.OnFileCreated;
            watcher.Changed += viewModel.OnFileChanged;
            watcher.Deleted += viewModel.OnFileDeleted;
            watcher.Renamed += viewModel.OnFileRenamed;
            watcher.EnableRaisingEvents = true;

          

        }
    

      
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var selectedWatchedFolder = (WatchedFolder)dataGrid.SelectedItem;

            if (selectedWatchedFolder != null)
            {
                // Open a dialog or navigate to a separate view to display the list of files
                ShowFilesDialog(selectedWatchedFolder);
            }
        }
        private void ShowFilesDialog(WatchedFolder watchedFolder)
        {
            // Path to the JSON file
            string jsonFilePath = System.IO.Path.Combine(watchedFolder.FolderPath, "files.json");

            if (File.Exists(jsonFilePath))
            {
                try
                {
                    // Read the JSON file and retrieve the list of files
                    List<string> files;
                    using (var fileStream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string jsonContent = reader.ReadToEnd();
                        files = JsonConvert.DeserializeObject<List<string>>(jsonContent);
                    }

                    if (files.Any())
                    {
                        // Create a new Window
                        var window = new Window
                        {
                            Title = watchedFolder.FolderPath, // Set the title as the FolderPath
                            Width = 400,
                            Height = 300,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize
                        };

                        // Create a Grid to hold the ListBox and buttons
                        var grid = new Grid
                        {
                            Margin = new Thickness(10)
                        };

                        // Create the columns for the Grid
                        var filePathColumn = new ColumnDefinition();
                        var actionColumn = new ColumnDefinition();
                        grid.ColumnDefinitions.Add(filePathColumn);
                        grid.ColumnDefinitions.Add(actionColumn);

                        // Create a TextBlock for the "File Path" header
                        var filePathHeader = new TextBlock
                        {
                            Text = "File Path",
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 0, 10, 0)
                        };
                        Grid.SetColumn(filePathHeader, 0);
                        grid.Children.Add(filePathHeader);

                        // Create a TextBlock for the "Action" header
                        var actionHeader = new TextBlock
                        {
                            Text = "Action",
                            FontWeight = FontWeights.Bold
                        };
                        Grid.SetColumn(actionHeader, 1);
                        grid.Children.Add(actionHeader);

                        // Create a ListBox to display the files
                        var listBox = new ListBox
                        {
                            Margin = new Thickness(0, 20, 0, 10)
                        };

                        // Create a collection to hold the custom objects (file path + action button)
                        var fileList = new ObservableCollection<FilePathItem>();

                        // Add the files to the collection
                        foreach (string file in files)
                        {
                            var button = new Button
                            {
                                Content = "Re-Upload",
                                Tag = file,
                                Margin = new Thickness(0, 5, 0, 0)
                            };
                            button.Click += ReUploadFile_Click;
                            fileList.Add(new FilePathItem { FilePath = file, ActionButton = button });
                        }

                        // Bind the collection to the ListBox's ItemsSource
                        listBox.ItemsSource = fileList;

                        // Create a DataTemplate for the ListBox to display the file path and action button
                        var itemTemplate = new DataTemplate();
                        var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));

                        // Create a TextBlock to display the file path
                        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding("FilePath"));
                        stackPanelFactory.AppendChild(textBlockFactory);

                        // Create a Button to display the action button
                        var buttonFactory = new FrameworkElementFactory(typeof(Button));
                        buttonFactory.SetBinding(Button.ContentProperty, new Binding("ActionButton.Content"));
                        buttonFactory.SetBinding(Button.TagProperty, new Binding("ActionButton.Tag"));
                        buttonFactory.SetBinding(Button.MarginProperty, new Binding("ActionButton.Margin"));
                        buttonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ReUploadFile_Click));
                        stackPanelFactory.AppendChild(buttonFactory);

                        // Set the StackPanel as the visual tree of the item template
                        itemTemplate.VisualTree = stackPanelFactory;
                        listBox.ItemTemplate = itemTemplate;

                        // Add the ListBox to the Grid
                        Grid.SetColumn(listBox, 0);
                        Grid.SetColumnSpan(listBox, 2);
                        grid.Children.Add(listBox);

                        // Add the Grid to the window's content
                        window.Content = grid;

                        // Show the window
                        window.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("No files found in the Watched Folder.", "Watched Folder Files", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error accessing the JSON file: {ex.Message}", "Watched Folder Files", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The JSON file does not exist in the Watched Folder.", "Watched Folder Files", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void ReUploadFile_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var filePath = (string)button.Tag;

            bool status = await viewModel.UploadFile(filePath);
            if (status)
            {
                MessageBox.Show("File re-uploaded successfully.", "Re-Upload Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to re-upload the file.", "Re-Upload Status", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LogEntriesDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Scroll to the last item in the LogEntriesDataGrid
            if (e.ExtentHeightChange > 0)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>((DependencyObject)sender);
                scrollViewer?.ScrollToBottom();
            }
        }

        private void WatchedFoldersDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Scroll to the last item in the WatchedFoldersDataGrid
            if (e.ExtentHeightChange > 0)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>((DependencyObject)sender);
                scrollViewer?.ScrollToBottom();
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }


    }
    public class FilePathItem
    {
        public string FilePath { get; set; }
        public Button ActionButton { get; set; }
    }

}
