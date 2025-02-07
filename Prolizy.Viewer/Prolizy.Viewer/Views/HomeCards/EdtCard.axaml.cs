using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.Viewer.Controls.Edt;

namespace Prolizy.Viewer.Views.HomeCards;

public partial class EdtCard : UserControl
{
    public EdtCard()
    {
        InitializeComponent();
        
        DataContext = new EdtCardViewModel();
    }
    
    public EdtCardViewModel ViewModel => ((EdtCardViewModel) DataContext)!;
}

public partial class EdtCardViewModel : ObservableObject
{

    [ObservableProperty] private ScheduleItem? _item;
    
    public IBrush BackgroundColor => Item == null 
        ? Brushes.Red 
        : new SolidColorBrush(Item.BackgroundColor);

    public EdtCardViewModel()
    {
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Item))
            {
                OnPropertyChanged(nameof(BackgroundColor));
            }
        };
    }

}