using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Prolizy.Viewer.Controls.Bulletin;

public partial class AverageDisplay : UserControl
{
    
    public enum DisplaySize
    {
        Small,
        Medium,
        Large
    }
    
    public static readonly StyledProperty<bool> IsAboveAverageProperty =
        AvaloniaProperty.Register<Card, bool>("IsAboveAverage", true);
    public static readonly StyledProperty<string> AverageProperty =
        AvaloniaProperty.Register<Card, string>("Average", "0");
    public static readonly StyledProperty<DisplaySize> SizeProperty =
        AvaloniaProperty.Register<Card, DisplaySize>("Size", DisplaySize.Medium);

    public bool IsAboveAverage
    {
        get => GetValue(IsAboveAverageProperty);
        set => SetValue(IsAboveAverageProperty, value);
    }
    
    public string Average
    {
        get => GetValue(AverageProperty);
        set => SetValue(AverageProperty, value);
    }
    
    public DisplaySize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    
    public AverageDisplay()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        DataContext = new AverageDisplayViewModel()
        {
            IsAboveAverage = IsAboveAverage,
            Average = Average,
            
            IconFontSize = Size switch
            {
                DisplaySize.Small => 20,
                DisplaySize.Medium => 22,
                DisplaySize.Large => 24,
                _ => 20
            },
            
            MainFontSize = Size switch
            {
                DisplaySize.Small => 14,
                DisplaySize.Medium => 16,
                DisplaySize.Large => 22,
                _ => 14
            },
            
            SubFontSize = Size switch
            {
                DisplaySize.Small => 12,
                DisplaySize.Medium => 14,
                DisplaySize.Large => 18,
                _ => 12
            }
        };
    }
    
}

public partial class AverageDisplayViewModel : ObservableObject
{
    
    [ObservableProperty] private bool _isAboveAverage;
    [ObservableProperty] private string _average;

    #region Sizes

    [ObservableProperty] private int _iconFontSize = 20;
    [ObservableProperty] private int _mainFontSize = 14;
    [ObservableProperty] private int _subFontSize = 12;

    #endregion

}