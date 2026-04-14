using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DiskSpaceMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll")]
        private static extern int SHObjectProperties(
            IntPtr hwnd, uint shopObjectType, [MarshalAs(UnmanagedType.LPWStr)] string pszObjectName, string pszPage);

        private const uint SHOP_FILEPATH = 0x2;

        private readonly FileDeletionLogger _logger;

        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            LoadDiskInfo();
            InitializeTempFolders();
            LoadTempFiles(tempFoldersComboBox.SelectedItem.ToString());

            _logger = new FileDeletionLogger();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(5);
            _timer.Tick += (s, e) => LoadDiskInfo();
            _timer.Start();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDiskInfo();
        }

        private void TempFoldersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTempFiles(tempFoldersComboBox.SelectedItem.ToString());
        }

        private void RefreshListButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTempFiles(tempFoldersComboBox.SelectedItem.ToString());
        }

        private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = tempFilesDataGrid.SelectedItems.Cast<TempFileInfo>().ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Выберите файлы для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Удалить {selectedItems.Count} выбранных файлов?", "Подтвердите действие",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                foreach (TempFileInfo file in selectedItems)
                {
                    try
                    {
                        File.Delete(file.FullPath);
                        _logger.LogSuccess(file.FullPath, Environment.UserName);
                        if (selectedItems.Count > 1)
                            MessageBox.Show("Выбранные файлы удалены", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        else
                            MessageBox.Show("Файл удалён", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch
                    {
                        _logger.LogFailure(file.FullPath, $"Файл используется системой");
                        MessageBox.Show($"Не удалось удалить файл {file.Name}: файл используется системой",
                            "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    LoadTempFiles(tempFoldersComboBox.SelectedItem.ToString());
                }
            }
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Удалить все временные файлы из папки {tempFoldersComboBox.Text}?", "Подтвердите действие", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string folderPath = tempFoldersComboBox.Text;
                    foreach (var filePath in Directory.GetFiles(folderPath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            _logger.LogSuccess(filePath, Environment.UserName);
                        }
                        catch (IOException)
                        {
                            _logger.LogFailure(filePath, $"Файл используется системой");
                        }
                        catch (UnauthorizedAccessException) { }
                    }
                    MessageBox.Show("Часть временных файлов была удалена успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    foreach (var dirPath in Directory.GetDirectories(folderPath))
                    {
                        try
                        {
                            Directory.Delete(dirPath, recursive: true);
                            _logger.LogSuccess(dirPath, Environment.UserName);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger.LogFailure(dirPath, $"У вас недостаточно прав");
                        }
                    }
                }
                catch { }
                LoadTempFiles(tempFoldersComboBox.SelectedItem.ToString());
            }
        }

        private void OpenInExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (disksDataGrid.SelectedItem is DiskInfo selectedDisk)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", selectedDisk.RootDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть в проводнике: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Выберите диск для открытия", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowPropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (disksDataGrid.SelectedItem is DiskInfo selectedDisk)
            {
                try
                {
                    SHObjectProperties(IntPtr.Zero, SHOP_FILEPATH, selectedDisk.RootDirectory, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось показать свойтсва: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeTempFolders()
        {
            tempFoldersComboBox.Items.Clear();
            tempFoldersComboBox.Items.Add(Path.GetTempPath());
            tempFoldersComboBox.Items.Add(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Temp"));
        }

        private void LoadDiskInfo()
        {
            var disks = new List<DiskInfo>();
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in allDrives)
            {
                if (drive.IsReady)
                {
                    double totalSizeGB = drive.TotalSize / (1024.0 * 1024 * 1024);
                    double freeSpaceGB = drive.TotalFreeSpace / (1024.0 * 1024 * 1024);
                    double usedSpaceGB = totalSizeGB - freeSpaceGB;
                    double freePercent = (freeSpaceGB / totalSizeGB) * 100;

                    disks.Add(new DiskInfo
                    {
                        Name = drive.Name,
                        DriveType = drive.DriveType.ToString(),
                        TotalSizeGB = $"{totalSizeGB:F2} GB",
                        FreeSpaceGB = $"{freeSpaceGB:F2} GB",
                        UsedSpaceGB = $"{usedSpaceGB:F2} GB",
                        FreePercent = freePercent,
                        RootDirectory = drive.RootDirectory.ToString()
                    });
                }
            }

            disksDataGrid.ItemsSource = disks;
        }

        private void LoadTempFiles(string folderPath)
        {
            tempFilesDataGrid.ItemsSource = null;
            var tempFiles = new List<TempFileInfo>();

            try
            {
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    tempFiles.Add(new TempFileInfo
                    {
                        Name = fileInfo.Name,
                        Size = FormatFileSize(fileInfo.Length),
                        LastWriteTime = fileInfo.LastWriteTime,
                        FullPath = filePath
                    });
                }
                tempFilesDataGrid.ItemsSource = tempFiles;
                UpdateTotalSize(tempFiles);
            }
            catch (Exception ex)
            {

            }

            if (tempFilesDataGrid.HasItems)
            {
                notAvailableTextBlock.Visibility = Visibility.Collapsed;
                deleteSelectedButton.IsEnabled = true;
                deleteAllButton.IsEnabled = true;
            }
            else
            {
                notAvailableTextBlock.Visibility = Visibility.Visible;
                deleteSelectedButton.IsEnabled = false;
                deleteAllButton.IsEnabled = false;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        private void UpdateTotalSize(List<TempFileInfo> files)
        {
            long totalSize = files.Sum(f => new FileInfo(f.FullPath).Length);
            totalSizeLabel.Content = FormatFileSize(totalSize);
        }
    }
}