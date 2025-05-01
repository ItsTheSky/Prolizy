using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Utils;

namespace Prolizy.Viewer.Controls.Edt;

public class ScheduleListControl : UserControl, IScheduleControl
{

    public static readonly StyledProperty<IEnumerable<ScheduleItem>> ItemsProperty =
        AvaloniaProperty.Register<ScheduleListControl, IEnumerable<ScheduleItem>>(nameof(Items));

    public IEnumerable<ScheduleItem> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    private readonly StackPanel _mainStack;

    public ScheduleListControl()
    {
        _mainStack = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(10)
        };

        Content = _mainStack;

        this.GetObservable(ItemsProperty).Subscribe(_ => UpdateVisual());
    }

    public void UpdateVisual()
    {
        _mainStack.Children.Clear();

        if (Items == null!)
            return;

        if (!Items.Any())
        {
            var noItemsBlock = new TextBlock
            {
                Text = "Aucun cours trouvé",
                FontSize = 18,
                Foreground = Brushes.Gray
            };

            _mainStack.Children.Add(noItemsBlock);
            return;
        }
            
        // sort items per start times
        var items = Items.OrderBy(x => x.StartTime).ToList();
        foreach (var item in items)
        {
            var itemContainer = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,10,*"),
                Margin = new Thickness(0, 5),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            
            itemContainer.Tapped += (sender, e) =>
            {
                Dispatcher.UIThread.InvokeAsync(async () => await item.ItemClicked());
            };

            // Colonne de temps
            var timeStack = new Grid
            {
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                RowDefinitions = new RowDefinitions("*,*")
            };

            var startTimeBlock = new TextBlock
            {
                Text = $"{item.StartTime:HH\\:mm}",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Top
            };

            var endTimeBlock = new TextBlock
            {
                Text = $"{item.EndTime:HH\\:mm}",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Bottom,
                FontWeight = FontWeight.DemiBold
            };

            timeStack.Children.Add(startTimeBlock);
            timeStack.Children.Add(endTimeBlock);
            Grid.SetRow(startTimeBlock, 0);
            Grid.SetRow(endTimeBlock, 1);

            // Barre verticale colorée
            var colorBar = new Border
            {
                Background = new SolidColorBrush(item.BackgroundColor),
                Width = 4,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = new CornerRadius(2)
            };

            var contentBorder = new Card();

            var contentGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto")
            };

            // Titre du cours
            var titleBlock = new TextBlock
            {
                Text = item.Subject,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };


            // Informations du professeur et de la salle
            var profRoomStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var professorBlock = new TextBlock
            {
                Text = item.Professor,
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var separatorBlock = new TextBlock
            {
                Text = "•",
                FontSize = 14,
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            //<Label Classes="Purple" Theme="{StaticResource TagLabel}">Purple</Label>
            var roomBlock = new Card
            {
                Content = item.Room,
                FontSize = 14,
                Classes = { "badge", "badge-accent" }
            };

            profRoomStack.Children.Add(roomBlock);
            profRoomStack.Children.Add(separatorBlock);
            profRoomStack.Children.Add(professorBlock);

            // Groupe
            var groupBlock = new TextBlock
            {
                Text = item.Group,
                FontSize = 14,
                Foreground = Brushes.Gray
            };

            contentGrid.Children.Add(titleBlock);
            Grid.SetRow(titleBlock, 0);

            contentGrid.Children.Add(profRoomStack);
            Grid.SetRow(profRoomStack, 1);

            contentGrid.Children.Add(groupBlock);
            Grid.SetRow(groupBlock, 2);

            contentBorder.Content = contentGrid;

            // Assemblage final
            Grid.SetColumn(timeStack, 0);
            Grid.SetColumn(colorBar, 1);
            Grid.SetColumn(contentBorder, 2);

            itemContainer.Children.Add(timeStack);
            itemContainer.Children.Add(colorBar);
            itemContainer.Children.Add(contentBorder);

            _mainStack.Children.Add(itemContainer);
        }
    }
}