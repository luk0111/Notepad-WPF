using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using Notepad__WPF.Commands;

namespace Notepad__WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TabDocument> Documents { get; }

        private TabDocument? _currentDocument;
        public TabDocument? CurrentDocument
        {
            get => _currentDocument;
            set { _currentDocument = value; OnPropertyChanged(nameof(CurrentDocument)); }
        }

        private double _editorFontSize = 14;
        public double EditorFontSize
        {
            get => _editorFontSize;
            set { _editorFontSize = Math.Clamp(value, 8, 72); OnPropertyChanged(nameof(EditorFontSize)); }
        }

        private TextWrapping _wordWrapMode = TextWrapping.NoWrap;
        public TextWrapping WordWrapMode
        {
            get => _wordWrapMode;
            set { _wordWrapMode = value; OnPropertyChanged(nameof(WordWrapMode)); }
        }

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand CloseCommand { get; }

        public MainViewModel(ObservableCollection<TabDocument>? documents,
                             Action? newAction,
                             Action? openAction,
                             Action? saveAction,
                             Action? saveAllAction,
                             Action? closeAction)
        {
            Documents = documents ?? new ObservableCollection<TabDocument>();

            NewCommand = new RelayCommand(_ => newAction?.Invoke());
            OpenCommand = new RelayCommand(_ => openAction?.Invoke());
            SaveCommand = new RelayCommand(_ => saveAction?.Invoke());
            SaveAllCommand = new RelayCommand(_ => saveAllAction?.Invoke());
            CloseCommand = new RelayCommand(_ => closeAction?.Invoke());
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
