using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
        
        Instance.TextBlock.Text += line + "\n";
    }
}