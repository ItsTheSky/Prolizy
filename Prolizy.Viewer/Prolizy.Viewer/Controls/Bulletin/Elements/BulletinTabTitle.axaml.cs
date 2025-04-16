using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace Prolizy.Viewer.Controls.Bulletin.Elements;

public partial class BulletinTabTitle : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<BulletinTabTitle, string>(nameof(Title));

    public static readonly StyledProperty<string> SubTitleProperty =
        AvaloniaProperty.Register<BulletinTabTitle, string>(nameof(SubTitle));

    public static readonly StyledProperty<bool> HasSpreadProperty =
        AvaloniaProperty.Register<BulletinTabTitle, bool>(nameof(HasSpread));
    
    public static readonly StyledProperty<RelayCommand> SpreadClickedCommandProperty =
        AvaloniaProperty.Register<BulletinTabTitle, RelayCommand>(nameof(SpreadClickedCommand));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string SubTitle
    {
        get => GetValue(SubTitleProperty);
        set => SetValue(SubTitleProperty, value);
    }
    
    public bool HasSpread
    {
        get => GetValue(HasSpreadProperty);
        set => SetValue(HasSpreadProperty, value);
    }
    
    public RelayCommand SpreadClickedCommand
    {
        get => GetValue(SpreadClickedCommandProperty);
        set => SetValue(SpreadClickedCommandProperty, value);
    }
    
    public BulletinTabTitle()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        DataContext = this;
    }
}