using System.Windows.Input;

namespace Notepad__WPF
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand Search = new(
            "Search",
            "Search",
            typeof(CustomCommands),
            new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control) });

        public static readonly RoutedUICommand Replace = new(
            "Replace",
            "Replace",
            typeof(CustomCommands),
            new InputGestureCollection { new KeyGesture(Key.H, ModifierKeys.Control) });

        public static readonly RoutedUICommand GoToLine = new(
            "Go To Line",
            "GoToLine",
            typeof(CustomCommands),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });

        public static readonly RoutedUICommand ZoomIn = new(
            "Zoom In",
            "ZoomIn",
            typeof(CustomCommands),
            new InputGestureCollection { new KeyGesture(Key.OemPlus, ModifierKeys.Control) });

        public static readonly RoutedUICommand ZoomOut = new(
            "Zoom Out",
            "ZoomOut",
            typeof(CustomCommands),
            new InputGestureCollection { new KeyGesture(Key.OemMinus, ModifierKeys.Control) });

        public static readonly RoutedUICommand WordWrap = new(
            "Word Wrap",
            "WordWrap",
            typeof(CustomCommands));
    }
}
