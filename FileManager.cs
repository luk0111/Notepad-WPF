using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Notepad__WPF
{
    public class FileManager
    {
        private ObservableCollection<TabDocument> _documents;
        private List<string> _recentFiles;
        private const int MaxRecentFiles = 10;
        private Action<TabDocument>? _selectTab;
        private Action? _updateStatusBar;
        private Action? _updateRecentFilesMenu;
        private Action? _createNewTab;
        private Action<string>? _openFile;
        private Action<string>? _saveFile;
        private Func<TabDocument?>? _getCurrentDocument;
        private MenuItem? _recentFilesMenu;

        public FileManager(
            ObservableCollection<TabDocument> documents,
            List<string> recentFiles,
            Action<TabDocument>? selectTab,
            Action? updateStatusBar,
            Action? updateRecentFilesMenu,
            Action? createNewTab,
            Action<string>? openFile,
            Action<string>? saveFile,
            Func<TabDocument?>? getCurrentDocument,
            MenuItem? recentFilesMenu)
        {
            _documents = documents;
            _recentFiles = recentFiles;
            _selectTab = selectTab;
            _updateStatusBar = updateStatusBar;
            _updateRecentFilesMenu = updateRecentFilesMenu;
            _createNewTab = createNewTab;
            _openFile = openFile;
            _saveFile = saveFile;
            _getCurrentDocument = getCurrentDocument;
            _recentFilesMenu = recentFilesMenu;
        }

        public void CreateNewTab(string? filePath = null, string content = "")
        {
            var doc = new TabDocument
            {
                FilePath = filePath,
                Content = content,
                Title = filePath != null ? Path.GetFileName(filePath) : "Untitled",
                IsModified = false
            };
            _documents.Add(doc);
            _selectTab?.Invoke(doc);
            _updateStatusBar?.Invoke();
        }

        public void OpenFile(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                CreateNewTab(filePath, content);
                AddToRecentFiles(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveFile(string path)
        {
            var doc = _getCurrentDocument?.Invoke();
            if (doc == null) return;
            try
            {
                File.WriteAllText(path, doc.Content, Encoding.UTF8);
                doc.IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveAll()
        {
            foreach (var doc in _documents)
            {
                if (doc.IsModified)
                {
                    _selectTab?.Invoke(doc);
                    SaveFile(doc.FilePath ?? "");
                }
            }
        }

        public void CloseDocument(TabDocument doc)
        {
            if (doc.IsModified)
            {
                var result = MessageBox.Show($"Save changes to {doc.Title}?", "Notepad++ WPF",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        _selectTab?.Invoke(doc);
                        SaveFile(doc.FilePath ?? "");
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
            _documents.Remove(doc);
            if (_documents.Count == 0)
                _createNewTab?.Invoke();
        }

        public void CloseAllTabs()
        {
            foreach (var doc in _documents.ToList())
            {
                CloseDocument(doc);
            }
        }

        public void AddToRecentFiles(string filePath)
        {
            _recentFiles.Remove(filePath);
            _recentFiles.Insert(0, filePath);
            if (_recentFiles.Count > MaxRecentFiles)
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            SaveRecentFiles();
            _updateRecentFilesMenu?.Invoke();
        }

        public void SaveRecentFiles()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotepadPlusPlusWPF");
                Directory.CreateDirectory(folder);
                File.WriteAllLines(Path.Combine(folder, "recent.txt"), _recentFiles);
            }
            catch { }
        }

        public void LoadRecentFiles()
        {
            try
            {
                string recentFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotepadPlusPlusWPF", "recent.txt");
                if (File.Exists(recentFilePath))
                {
                    _recentFiles = File.ReadAllLines(recentFilePath).Where(File.Exists).Take(MaxRecentFiles).ToList();
                }
            }
            catch { }
            _updateRecentFilesMenu?.Invoke();
        }

        public void UpdateRecentFilesMenu()
        {
            if (_recentFilesMenu == null) return;
            _recentFilesMenu.Items.Clear();
            if (_recentFiles.Count == 0)
            {
                var emptyItem = new MenuItem { Header = "(Empty)", IsEnabled = false };
                _recentFilesMenu.Items.Add(emptyItem);
            }
            else
            {
                foreach (var file in _recentFiles)
                {
                    var menuItem = new MenuItem
                    {
                        Header = Path.GetFileName(file),
                        ToolTip = file,
                        Tag = file
                    };
                    menuItem.Click += (s, e) => OpenFile(file);
                    _recentFilesMenu.Items.Add(menuItem);
                }
                _recentFilesMenu.Items.Add(new Separator());
                var clearItem = new MenuItem { Header = "Clear Recent Files" };
                clearItem.Click += (s, e) =>
                {
                    _recentFiles.Clear();
                    SaveRecentFiles();
                    UpdateRecentFilesMenu();
                };
                _recentFilesMenu.Items.Add(clearItem);
            }
        }

        public void OpenFileDialog()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|C# Files (*.cs)|*.cs|XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                OpenFile(dialog.FileName);
            }
        }

        public void SaveCurrentDocument()
        {
            var doc = _getCurrentDocument?.Invoke();
            if (doc == null) return;
            if (string.IsNullOrEmpty(doc.FilePath))
                SaveAsCurrentDocument();
            else
                SaveFile(doc.FilePath);
        }

        public void SaveAsCurrentDocument()
        {
            var doc = _getCurrentDocument?.Invoke();
            if (doc == null) return;
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|C# Files (*.cs)|*.cs|XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                SaveFile(dialog.FileName);
                doc.FilePath = dialog.FileName;
                doc.Title = Path.GetFileName(dialog.FileName);
                AddToRecentFiles(dialog.FileName);
            }
        }

        public void CloseCurrentDocument()
        {
            var doc = _getCurrentDocument?.Invoke();
            if (doc == null) return;
            CloseDocument(doc);
        }
    }
}
