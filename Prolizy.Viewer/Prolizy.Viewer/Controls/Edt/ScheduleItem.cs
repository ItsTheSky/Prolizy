using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Controls.Edt;

public class ScheduleItem
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Professor { get; set; }
    public string Subject { get; set; }
    public string Room { get; set; }
    public string Group { get; set; }

    public ScheduleItemOverlay? Overlay { get; set; }

    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public Color BorderColor { get; set; }

    public Course Course { get; set; }

    private readonly List<ContentDialog> _dialogs = new();

    public async Task ItemClicked()
    {
        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new TextBlock
                {
                    Text = Subject,
                    FontWeight = FontWeight.SemiBold, TextAlignment = TextAlignment.Center, FontSize = 18
                },
                Utilities.Controls.CreateDataGrid(new Dictionary<string, string>()
                {
                    //{ "Matière", Subject }, // already displayed in the title now
                    { "Salle", Room },
                    { "Professeur", Professor },
                    { "Horaires", $@"{StartTime:HH\:mm} - {EndTime:HH\:mm}" }
                }, rowSpacing: 5)
            },
            Margin = new Thickness(5, 0)
        };

        var dialog = new ContentDialog
        {
            Title = "Informations",
            Content = content,
            CloseButtonText = "Fermer"
        };
        dialog.Closed += (_, _) => _dialogs.Clear();
        dialog.Opened += async (_, _) =>
        {
            _dialogs.Add(dialog);
            if (_dialogs.Count >= 5)
            {
                foreach (var d in _dialogs)
                    d.Hide();
                
                _dialogs.Clear();
                await Task.Delay(100);
                await Dialogs.ShowMessage("Oh oh ...",
                    """
                    Vous venez de trouvez un easter-egg !

                    Il faut croire que vous aimez vraiment beaucoup ce cours :')
                    """);
            }
        };

        await dialog.ShowAsync();
    }
}

public record ScheduleItemOverlay(IBrush Overlay, IBrush TextColor, string DisplayText);