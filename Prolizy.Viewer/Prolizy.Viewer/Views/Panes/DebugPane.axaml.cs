using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Views.Panes;

public partial class DebugPane : UserControl
{
    public static DebugPane Instance { get; set; }
    
    public DebugPane()
    {
        InitializeComponent();
        
        Instance = this;
    }

    public static void AddDebugText(string line)
    {
        if (Instance == null!)
            return;
        
        // Ensure we're on the UI thread
        Dispatcher.UIThread.Post(() => Instance.TextBlock.Text += line + "\n");
    }

    private string _storedLogs = "";
    private void LoadErrorFile_OnClick(object? sender, RoutedEventArgs e)
    {
        var btn = (Button) sender!;
        var errorFile = Paths.Build("error.txt");
        if (File.Exists(errorFile))
        {
            _storedLogs = Instance.TextBlock.Text ?? "";
            var errorLogs = File.ReadAllText(errorFile);
            Instance.TextBlock.Text = errorLogs;
            
            btn.Content = "Back";
            btn.Click -= LoadErrorFile_OnClick;
            btn.Click += RestoreLogs_OnClick;
        }
        else
        {
            AddDebugText("No error file found!");
        }
    }
    
    private void RestoreLogs_OnClick(object? sender, RoutedEventArgs e)
    {
        var btn = (Button) sender!;
        Instance.TextBlock.Text = _storedLogs;
        
        btn.Content = "Charger le rapport d'erreur";
        btn.Click -= RestoreLogs_OnClick;
        btn.Click += LoadErrorFile_OnClick;
    }

    private void Clear_OnClick(object? sender, RoutedEventArgs e)
    {
        Instance.TextBlock.Text = "";
    }
    
    private void Crash_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test");
    }
    
    private async void Copy_OnClick(object? sender, RoutedEventArgs e)
    {
        await MainView.Instance.Clipboard.SetTextAsync(Instance.TextBlock.Text);
        MainView.ShowNotification("Succès!", "Copié dans le presse-papiers!", NotificationType.Success);
    }
}