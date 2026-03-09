using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Notepad__WPF;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<TabDocument> Documents { get; } = [];
    private List<string> _recentFiles = [];
    private const int MaxRecentFiles = 10;
    
    private double _editorFontSize = 14;
    public double EditorFontSize
    {
        get => _editorFontSize;
        set
        {
            _editorFontSize = Math.Clamp(value, 8, 72);
            OnPropertyChanged(nameof(EditorFontSize));
            UpdateZoomDisplay();
        }
    }
    
    private TextWrapping _wordWrapMode = TextWrapping.NoWrap;
    public TextWrapping WordWrapMode
    {
        get => _wordWrapMode;
        set
        {
            _wordWrapMode = value;
            OnPropertyChanged(nameof(WordWrapMode));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        TabContainer.ItemsSource = Documents;
        CreateNewTab();
        LoadRecentFiles();
    }

    private TabDocument? CurrentDocument => TabContainer.SelectedItem as TabDocument;

    private void CreateNewTab(string? filePath = null, string content = "")
    {
        var doc = new TabDocument
        {
            FilePath = filePath,
            Content = content,
            Title = filePath != null ? Path.GetFileName(filePath) : "Untitled",
            IsModified = false
        };
        Documents.Add(doc);
        TabContainer.SelectedItem = doc;
        UpdateStatusBar();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        CreateNewTab();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|C# Files (*.cs)|*.cs|XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            OpenFile(dialog.FileName);
        }
    }

    private void OpenFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            CreateNewTab(filePath, content);
            AddToRecentFiles(filePath);
            StatusText.Text = $"Opened: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null) return;

        if (string.IsNullOrEmpty(CurrentDocument.FilePath))
            SaveAs_Click(sender, e);
        else
            SaveFile(CurrentDocument.FilePath);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|C# Files (*.cs)|*.cs|XML Files (*.xml)|*.xml|JSON Files (*.json)|*.json|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            SaveFile(dialog.FileName);
            CurrentDocument.FilePath = dialog.FileName;
            CurrentDocument.Title = Path.GetFileName(dialog.FileName);
            AddToRecentFiles(dialog.FileName);
        }
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var doc in Documents)
        {
            if (doc.IsModified)
            {
                TabContainer.SelectedItem = doc;
                Save_Click(sender, e);
            }
        }
        StatusText.Text = "All files saved";
    }

    private void SaveFile(string path)
    {
        if (CurrentDocument == null) return;

        try
        {
            File.WriteAllText(path, CurrentDocument.Content, Encoding.UTF8);
            CurrentDocument.IsModified = false;
            StatusText.Text = $"Saved: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null) return;
        CloseDocument(CurrentDocument);
    }

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TabDocument doc)
        {
            CloseDocument(doc);
        }
    }

    private void CloseDocument(TabDocument doc)
    {
        if (doc.IsModified)
        {
            var result = MessageBox.Show($"Save changes to {doc.Title}?", "Notepad++ WPF",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    TabContainer.SelectedItem = doc;
                    Save_Click(this, new RoutedEventArgs());
                    break;
                case MessageBoxResult.Cancel:
                    return;
            }
        }

        Documents.Remove(doc);

        if (Documents.Count == 0)
            CreateNewTab();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        foreach (var doc in Documents.ToList())
        {
            TabContainer.SelectedItem = doc;
            if (doc.IsModified)
            {
                var result = MessageBox.Show($"Save changes to {doc.Title}?", "Notepad++ WPF",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Save_Click(this, new RoutedEventArgs());
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }
        }
        Close();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (CurrentDocument != null)
        {
            CurrentDocument.IsModified = true;
            UpdateStatusBar();
        }
    }

    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox editor)
        {
            int line = editor.GetLineIndexFromCharacterIndex(editor.CaretIndex) + 1;
            int col = editor.CaretIndex - editor.GetCharacterIndexFromLineIndex(line - 1) + 1;
            LineColText.Text = $"Ln {line}, Col {col}";
        }
    }

    private void TabContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentDocument != null)
        {
            Title = $"{CurrentDocument.Title}{(CurrentDocument.IsModified ? "*" : "")} - Notepad++ WPF";
            UpdateStatusBar();
            UpdateFileTypeDisplay();
        }
    }

    private void UpdateStatusBar()
    {
        if (CurrentDocument != null)
        {
            CharCountText.Text = $"{CurrentDocument.Content.Length} characters";
        }
    }

    private void UpdateFileTypeDisplay()
    {
        if (CurrentDocument?.FilePath != null)
        {
            string ext = Path.GetExtension(CurrentDocument.FilePath).ToLower();
            FileTypeText.Text = ext switch
            {
                ".cs" => "C#",
                ".txt" => "Plain Text",
                ".xml" => "XML",
                ".json" => "JSON",
                ".html" => "HTML",
                ".css" => "CSS",
                ".js" => "JavaScript",
                ".py" => "Python",
                ".md" => "Markdown",
                _ => "Plain Text"
            };
        }
        else
        {
            FileTypeText.Text = "Plain Text";
        }
    }

    private void AddTab_Click(object sender, RoutedEventArgs e)
    {
        CreateNewTab();
    }

    private void New_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        CreateNewTab();
    }

    private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Open_Click(sender, e);
    }

    private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Save_Click(sender, e);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Notepad++ WPF Clone\n\nRusu Luca-Andrei\n10LF243\nluca.rusu@student.untibv.ro",
            "About Notepad++ WPF",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Search_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowSearchPanel(replaceMode: false);
    }

    private void Replace_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowSearchPanel(replaceMode: true);
    }

    private void ShowSearchPanel(bool replaceMode)
    {
        SearchPanel.Visibility = Visibility.Visible;
        GoToLinePanel.Visibility = Visibility.Collapsed;
        
        ReplaceLabel.Visibility = replaceMode ? Visibility.Visible : Visibility.Collapsed;
        ReplaceTextBox.Visibility = replaceMode ? Visibility.Visible : Visibility.Collapsed;
        ReplaceButtons.Visibility = replaceMode ? Visibility.Visible : Visibility.Collapsed;
        
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindText(forward: true);
    }

    private void FindPrevious_Click(object sender, RoutedEventArgs e)
    {
        FindText(forward: false);
    }

    private void FindText(bool forward)
    {
        if (CurrentDocument == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

        var editor = GetCurrentEditor();
        if (editor == null) return;

        int startIndex;
        int index;

        if (forward)
        {
            startIndex = editor.SelectionStart + editor.SelectionLength;
            index = CurrentDocument.Content.IndexOf(SearchTextBox.Text, startIndex, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                index = CurrentDocument.Content.IndexOf(SearchTextBox.Text, 0, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            startIndex = Math.Max(0, editor.SelectionStart - 1);
            index = CurrentDocument.Content.LastIndexOf(SearchTextBox.Text, startIndex, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                index = CurrentDocument.Content.LastIndexOf(SearchTextBox.Text, CurrentDocument.Content.Length - 1, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (index >= 0)
        {
            editor.Select(index, SearchTextBox.Text.Length);
            editor.Focus();
            StatusText.Text = "Match found";
        }
        else
        {
            StatusText.Text = "No matches found";
        }
    }

    private void CountMatches_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

        int count = 0;
        int index = 0;
        while ((index = CurrentDocument.Content.IndexOf(SearchTextBox.Text, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += SearchTextBox.Text.Length;
        }

        StatusText.Text = $"{count} matches found";
        MessageBox.Show($"Found {count} occurrences of \"{SearchTextBox.Text}\"", "Count", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

        var editor = GetCurrentEditor();
        if (editor == null) return;

        if (editor.SelectedText.Equals(SearchTextBox.Text, StringComparison.OrdinalIgnoreCase))
        {
            int start = editor.SelectionStart;
            CurrentDocument.Content = CurrentDocument.Content.Remove(start, SearchTextBox.Text.Length)
                .Insert(start, ReplaceTextBox.Text);
            editor.Select(start, ReplaceTextBox.Text.Length);
            StatusText.Text = "Replaced";
        }
        
        FindText(forward: true);
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null || string.IsNullOrEmpty(SearchTextBox.Text)) return;

        int count = 0;
        string content = CurrentDocument.Content;
        int index = 0;
        
        while ((index = content.IndexOf(SearchTextBox.Text, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            content = content.Remove(index, SearchTextBox.Text.Length).Insert(index, ReplaceTextBox.Text);
            index += ReplaceTextBox.Text.Length;
            count++;
        }

        CurrentDocument.Content = content;
        StatusText.Text = $"Replaced {count} occurrences";
        MessageBox.Show($"Replaced {count} occurrences", "Replace All", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CloseSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchPanel.Visibility = Visibility.Collapsed;
    }

    private void GoToLine_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SearchPanel.Visibility = Visibility.Collapsed;
        GoToLinePanel.Visibility = Visibility.Visible;
        GoToLineTextBox.Focus();
        GoToLineTextBox.SelectAll();
    }

    private void GoToLine_Click(object sender, RoutedEventArgs e)
    {
        GoToLineNumber();
    }

    private void GoToLineTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            GoToLineNumber();
        }
        else if (e.Key == Key.Escape)
        {
            CloseGoToLine_Click(sender, e);
        }
    }

    private void GoToLineNumber()
    {
        if (CurrentDocument == null) return;

        if (int.TryParse(GoToLineTextBox.Text, out int lineNumber))
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            string[] lines = CurrentDocument.Content.Split('\n');
            lineNumber = Math.Clamp(lineNumber, 1, lines.Length);

            int charIndex = 0;
            for (int i = 0; i < lineNumber - 1; i++)
            {
                charIndex += lines[i].Length + 1;
            }

            editor.CaretIndex = charIndex;
            editor.Focus();
            GoToLinePanel.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Moved to line {lineNumber}";
        }
    }

    private void CloseGoToLine_Click(object sender, RoutedEventArgs e)
    {
        GoToLinePanel.Visibility = Visibility.Collapsed;
    }

    private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        EditorFontSize += 2;
    }

    private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        EditorFontSize -= 2;
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        EditorFontSize = 14;
    }

    private void UpdateZoomDisplay()
    {
        int zoomPercent = (int)Math.Round((_editorFontSize / 14.0) * 100);
        ZoomLevelText.Text = $"{zoomPercent}%";
        ZoomStatusText.Text = $"{zoomPercent}%";
    }

    private void WordWrap_Click(object sender, RoutedEventArgs e)
    {
        WordWrapMode = WordWrapMenuItem.IsChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
        StatusText.Text = WordWrapMenuItem.IsChecked ? "Word wrap enabled" : "Word wrap disabled";
    }

    private void WordWrap_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        WordWrapMenuItem.IsChecked = !WordWrapMenuItem.IsChecked;
        WordWrap_Click(sender, e);
    }

    private void DeleteLine_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null) return;
        var editor = GetCurrentEditor();
        if (editor == null) return;

        int lineIndex = editor.GetLineIndexFromCharacterIndex(editor.CaretIndex);
        int lineStart = editor.GetCharacterIndexFromLineIndex(lineIndex);
        int lineLength = editor.GetLineLength(lineIndex);
        
        if (lineStart + lineLength < CurrentDocument.Content.Length)
            lineLength++; // Include newline

        CurrentDocument.Content = CurrentDocument.Content.Remove(lineStart, lineLength);
        editor.CaretIndex = Math.Min(lineStart, CurrentDocument.Content.Length);
        StatusText.Text = "Line deleted";
    }

    private void DuplicateLine_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentDocument == null) return;
        var editor = GetCurrentEditor();
        if (editor == null) return;

        int lineIndex = editor.GetLineIndexFromCharacterIndex(editor.CaretIndex);
        int lineStart = editor.GetCharacterIndexFromLineIndex(lineIndex);
        int lineLength = editor.GetLineLength(lineIndex);
        
        string line = CurrentDocument.Content.Substring(lineStart, lineLength);
        int insertPos = lineStart + lineLength;
        
        if (insertPos < CurrentDocument.Content.Length && CurrentDocument.Content[insertPos] == '\n')
            insertPos++;

        CurrentDocument.Content = CurrentDocument.Content.Insert(insertPos, line + "\n");
        StatusText.Text = "Line duplicated";
    }

    private TextBox? GetCurrentEditor()
    {
        if (TabContainer.SelectedIndex < 0) return null;
        
        var container = TabContainer.ItemContainerGenerator.ContainerFromIndex(TabContainer.SelectedIndex) as TabItem;
        if (container == null) return null;

        var contentPresenter = FindVisualChild<ContentPresenter>(container);
        if (contentPresenter == null) return null;

        contentPresenter.ApplyTemplate();
        return FindVisualChild<TextBox>(contentPresenter);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            
            var childResult = FindVisualChild<T>(child);
            if (childResult != null)
                return childResult;
        }
        return null;
    }

    private void LoadRecentFiles()
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
        
        UpdateRecentFilesMenu();
    }

    private void SaveRecentFiles()
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotepadPlusPlusWPF");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "recent.txt"), _recentFiles);
        }
        catch { }
    }

    private void AddToRecentFiles(string filePath)
    {
        _recentFiles.Remove(filePath);
        _recentFiles.Insert(0, filePath);
        if (_recentFiles.Count > MaxRecentFiles)
            _recentFiles.RemoveAt(_recentFiles.Count - 1);
        
        SaveRecentFiles();
        UpdateRecentFilesMenu();
    }

    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        
        if (_recentFiles.Count == 0)
        {
            var emptyItem = new MenuItem { Header = "(Empty)", IsEnabled = false };
            RecentFilesMenu.Items.Add(emptyItem);
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
                menuItem.Click += RecentFile_Click;
                RecentFilesMenu.Items.Add(menuItem);
            }
            
            RecentFilesMenu.Items.Add(new Separator());
            var clearItem = new MenuItem { Header = "Clear Recent Files" };
            clearItem.Click += (s, e) =>
            {
                _recentFiles.Clear();
                SaveRecentFiles();
                UpdateRecentFilesMenu();
            };
            RecentFilesMenu.Items.Add(clearItem);
        }
    }

    private void RecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string filePath)
        {
            if (File.Exists(filePath))
            {
                OpenFile(filePath);
            }
            else
            {
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _recentFiles.Remove(filePath);
                SaveRecentFiles();
                UpdateRecentFilesMenu();
            }
        }
    }
}

public class TabDocument : INotifyPropertyChanged
{
    private string? _filePath;
    private string _content = "";
    private string _title = "Untitled";
    private bool _isModified;

    public string? FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(nameof(FilePath)); }
    }

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(nameof(Content)); }
    }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(nameof(Title)); }
    }

    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(nameof(IsModified)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}