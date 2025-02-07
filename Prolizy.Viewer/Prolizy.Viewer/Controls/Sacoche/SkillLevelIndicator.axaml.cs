using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Controls.Sacoche;

public partial class SkillLevelIndicator : UserControl
{
    public static readonly StyledProperty<int?> LevelProperty =
        AvaloniaProperty.Register<SkillLevelIndicator, int?>(
            nameof(Level), defaultValue: -1, coerce: (_, value) => value == null ? null : Math.Clamp((int) value, -1, 4));

    public int? Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["red"]));
    private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["green"]));
    private static readonly IBrush UnknownBrush = new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["gray"]));

    public SkillLevelIndicator()
    {
        InitializeComponent();

        this.GetObservable(LevelProperty).Subscribe(UpdateCircles);
    }

    private void UpdateCircles(int? level)
    {
        switch (level)
        {
            case 1: // Deux rouges
                Circle1.Foreground = RedBrush;
                Circle2.Foreground = RedBrush;
                break;
            case 2: // Un rouge
                Circle1.Foreground = RedBrush;
                Circle2.IsVisible = false;
                break;
            case 3: // Un vert
                Circle1.Foreground = GreenBrush;
                Circle2.IsVisible = false;
                break;
            case 4: // Deux verts
                Circle1.Foreground = GreenBrush;
                Circle2.Foreground = GreenBrush;
                break;
            case -1: // NE: Non évalué
            case null:
                Circle1.IsVisible = true;
                Circle2.IsVisible = true;
                Circle1.Foreground = UnknownBrush;
                Circle2.Foreground = UnknownBrush;
                break;
        }
    }
}