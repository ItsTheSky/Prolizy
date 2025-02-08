using System;
using System.Threading.Tasks;
using FluentIcons.Common;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.HomeCards;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Cards;

public class EdtCardDefinition : CardDefinition
{
    public override Type CardType => typeof(EdtCard);
    public override string Id => "edt_current";
    public override string DisplayName => "Emploi du temps";
    public override string Description => "Affiche le prochain (ou l'actuel) cours de l'emploi du temps.";
    public override Symbol Symbol => Symbol.Calendar;
    public override string ModuleId => "edt";

    public override bool IsAvailable()
    {
        return Settings.Instance.EnabledModules.Contains("edt");
    }

    public override async Task UpdateCard(object raw)
    {
        var card = (EdtCard) raw;
        var (item, _) = await TimeTableViewModel.GetCurrentOrNextCourse();
        Console.WriteLine("Updating EDT card! Current course: " + item?.Subject);
        if (item == null)
        {
            card.ViewModel.Item = null;
            return;
        }
        
        card.ViewModel.Item = item;
    }
}