using Avalonia;
using Avalonia.Controls.Primitives;
using Prolizy.Viewer.Controls.Bulletin.Elements;

namespace Prolizy.Viewer.Controls.Bulletin.Elements;

public partial class BulletinAbsenceSumUp : TemplatedControl
{
    public static readonly StyledProperty<int> HalfJustifiedDayProperty =
        AvaloniaProperty.Register<BulletinTabTitle, int>(nameof(HalfJustifiedDay));
    
    public static readonly StyledProperty<int> HalfNotJustifiedDayProperty =
        AvaloniaProperty.Register<BulletinTabTitle, int>(nameof(HalfNotJustifiedDay));
    
    public static readonly StyledProperty<int> TotalHalfDaysProperty =
        AvaloniaProperty.Register<BulletinTabTitle, int>(nameof(TotalHalfDays));
    
    public static readonly StyledProperty<int> TotalRetardsProperty =
        AvaloniaProperty.Register<BulletinTabTitle, int>(nameof(TotalRetards));
    
    public int HalfJustifiedDay
    {
        get => GetValue(HalfJustifiedDayProperty);
        set => SetValue(HalfJustifiedDayProperty, value);
    }
    
    public int HalfNotJustifiedDay
    {
        get => GetValue(HalfNotJustifiedDayProperty);
        set => SetValue(HalfNotJustifiedDayProperty, value);
    }
    
    public int TotalHalfDays
    {
        get => GetValue(TotalHalfDaysProperty);
        set => SetValue(TotalHalfDaysProperty, value);
    }

    public int TotalRetards
    {
        get => GetValue(TotalRetardsProperty);
        set => SetValue(TotalRetardsProperty, value);
    }
}