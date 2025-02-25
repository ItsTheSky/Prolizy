using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FluentIcons.Avalonia.Fluent;
using FluentIcons.Common;
using Prolizy.API;

namespace Prolizy.Viewer.Controls.Edt;

public class DailyScheduleControl : UserControl, IScheduleControl
{
    public static readonly StyledProperty<IEnumerable<ScheduleItem>> ItemsProperty =
        AvaloniaProperty.Register<DailyScheduleControl, IEnumerable<ScheduleItem>>(nameof(Items));

    public static readonly StyledProperty<bool> ShowHourLinesProperty =
        AvaloniaProperty.Register<DailyScheduleControl, bool>(nameof(ShowHourLines), true);

    public IEnumerable<ScheduleItem> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public bool ShowHourLines
    {
        get => GetValue(ShowHourLinesProperty);
        set => SetValue(ShowHourLinesProperty, value);
    }

    private Grid _mainGrid;
    private Canvas _scheduleCanvas;
    private const int DisplayedHours = 11;
    private const int StartHour = 8;

    public DailyScheduleControl()
    {
        InitializeComponent();
    }

    public void UpdateVisual()
    {
        _scheduleCanvas.Children.Clear();
        if (_noData != null)
        {
            _mainGrid.Children.Remove(_noData);
            _noData = null;
        }

        if (Items == null!)
            return;

        var canvasHeight = _scheduleCanvas.Bounds.Height;
        var hourHeight = canvasHeight / DisplayedHours;

        // Draw hour lines if enabled
        if (ShowHourLines && !Items.Any(x => x.Type.IsHoliday()) && Items.Any())
        {
            for (var i = 0; i <= DisplayedHours; i++)
            {
                var y = i * hourHeight;
                var line = new Line
                {
                    StartPoint = new Point(0, y),
                    EndPoint = new Point(_scheduleCanvas.Bounds.Width, y),
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = [2, 2]
                };
                _scheduleCanvas.Children.Add(line);
            }
        }

        foreach (var item in Items)
        {
            var startY = (item.StartTime.TimeOfDay.TotalHours - StartHour) * hourHeight;
            var endY = (item.EndTime.TimeOfDay.TotalHours - StartHour) * hourHeight;
            var itemHeight = endY - startY;

            if (startY + itemHeight > canvasHeight)
            {
                itemHeight = canvasHeight - startY;
            }
            if (startY < 0)
            {
                itemHeight += startY;
                startY = 0;
            }

            if (itemHeight <= 0) continue; // Ignore les éléments hors des limites

            StackPanel content;
            if (!item.Type.IsHoliday())
            {
                content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = item.Subject, Foreground = new SolidColorBrush(item.ForegroundColor),
                            FontWeight = FontWeight.SemiBold, TextAlignment = TextAlignment.Center, FontSize = 18
                        },
                        Utilities.Controls.CreateDataGrid(new Dictionary<string, string>()
                        {
                            { "Matière", item.Subject },
                            { "Salle", item.Room },
                            { "Professeur", item.Professor },
                            { "Horaires", $@"{item.StartTime:HH\:mm} - {item.EndTime:HH\:mm}" }
                        }, item.ForegroundColor, rowSpacing: 5)
                    },
                    Margin = new Thickness(5, 0)
                };
            }
            else
            {
                content = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new SymbolIcon
                        {
                            Symbol = Symbol.Sleep,
                            Foreground = new SolidColorBrush(item.ForegroundColor),
                            FontSize = 32
                        },
                        new TextBlock
                        {
                            Text = "Jour férié/Vacance", Foreground = new SolidColorBrush(item.ForegroundColor),
                            FontWeight = FontWeight.SemiBold, TextAlignment = TextAlignment.Center, FontSize = 18
                        }
                    },
                    Margin = new Thickness(5, 0)
                };
            }
            var actualContent = new Grid();
            
            actualContent.Children.Add(content);
            if (item.Overlay != null)
            {
                var txt = new TextBlock
                {
                    Text = item.Overlay.DisplayText,
                    Foreground = item.Overlay.TextColor,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                actualContent.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#7F000000")),
                    Child = txt,
                    Width = _scheduleCanvas.Bounds.Width,
                    Height = itemHeight,
                    Padding = new Thickness(5),
                    CornerRadius = new CornerRadius(5),
                    ClipToBounds = true
                });
            }

            var button = new Border
            {
                Background = new SolidColorBrush(item.BackgroundColor),
                Child = actualContent,
                Width = _scheduleCanvas.Bounds.Width,
                Height = itemHeight,
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(5),
                ClipToBounds = true,
                
                BorderBrush = item.Overlay?.Overlay ?? new SolidColorBrush(item.BorderColor),
                BorderThickness = new Thickness(item.Overlay == null ? 2 : 5)
            };
            if (!item.Type.IsHoliday())
            {
                button.Cursor = new Cursor(StandardCursorType.Hand);
                button.Tapped += (sender, args) =>
                {
                    Dispatcher.UIThread.InvokeAsync(async () => await item.ItemClicked());
                };
            }

            Canvas.SetTop(button, startY);
            _scheduleCanvas.Children.Add(button);
        }

        if (!Items.Any())
        {
            _noData = new TextBlock
            {
                Text = "Aucun cours",
                Foreground = Brushes.Gray,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18
            };
            
            _mainGrid.Children.Add(_noData);
            Grid.SetColumn(_noData, 1);
            Grid.SetRowSpan(_noData, DisplayedHours);
        }
    }
    
    private static TextBlock? _noData = null;

    private void InitializeComponent()
    {
        _mainGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions(string.Join(",", Enumerable.Repeat("*", DisplayedHours+1)))
        };

        // Add time labels
        for (int i = 0; i <= DisplayedHours; i++)
        {
            var hour = StartHour + i;
            var timeLabel = new TextBlock
            {
                Text = $"{hour:D2}h",
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5, i == 0 ? -3 : -8, 5, 5)
            };
            Grid.SetRow(timeLabel, i);
            Grid.SetColumn(timeLabel, 0);
            _mainGrid.Children.Add(timeLabel);
        }

        // Add a Canvas for schedule items
        _scheduleCanvas = new Canvas();
        Grid.SetColumn(_scheduleCanvas, 1);
        Grid.SetRowSpan(_scheduleCanvas, DisplayedHours);
        _mainGrid.Children.Add(_scheduleCanvas);

        Content = _mainGrid;

        this.GetObservable(ItemsProperty).Subscribe(_ => UpdateVisual());
        this.GetObservable(ShowHourLinesProperty).Subscribe(_ => UpdateVisual());
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateVisual();
    }
}