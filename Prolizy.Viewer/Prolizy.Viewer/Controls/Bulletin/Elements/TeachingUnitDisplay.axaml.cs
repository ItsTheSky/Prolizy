using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Controls.Bulletin.Elements;

public partial class TeachingUnitDisplay : UserControl
{
    public TeachingUnitDisplay()
    {
        InitializeComponent();
    }
}

public partial class InternalTeachingUnit : ObservableObject
{
    public InternalTeachingUnit(BulletinRoot root, TeachingUnit teachingUnit, string unitId)
    {
        TeachingUnit = teachingUnit;
        Title = unitId;
        
        foreach (var entry in teachingUnit.Resources.Keys)
            Entries.Add(new InternalTeachingUnitEntry(root, teachingUnit, entry));
        foreach (var entry in teachingUnit.Saes.Keys)
            Entries.Add(new InternalTeachingUnitEntry(root, teachingUnit, entry, true));
    }

    [ObservableProperty] private TeachingUnit _teachingUnit;
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _isExpanded = true;

    public SolidColorBrush AccentBrush => new SolidColorBrush(ColorMatcher.FindClosestColor(TeachingUnit.Color));
    public SolidColorBrush TextBrush => AccentBrush.Brighten(1.5);

    [ObservableProperty] private ObservableCollection<InternalTeachingUnitEntry> _entries = [];
    
    public string DisplayTitle => $"{Title} - {TeachingUnit.Title}";

}

public record InternalTeachingUnitEntryData(string Title, string Average, int Coefficient, bool IsSae = false);

public partial class InternalTeachingUnitEntry : ObservableObject
{
    
    [ObservableProperty] private TeachingUnit _teachingUnit;
    [ObservableProperty] private InternalTeachingUnitEntryData _data;

    public InternalTeachingUnitEntry(BulletinRoot bulletinRoot, 
        TeachingUnit teachingUnit, string key, 
        bool isSae = false)
    {
        TeachingUnit = teachingUnit;
        if (isSae)
        {
            var sae = bulletinRoot.Transcript.Saes[key];
            var unitSae = teachingUnit.Saes[key];
            
            Data = new InternalTeachingUnitEntryData(
                sae.Title,
                unitSae.Average,
                unitSae.Coefficient,
                true
            );
        }
        else
        {
            var resource = bulletinRoot.Transcript.Resources[key];
            var unitResource = teachingUnit.Resources[key];
            
            Data = new InternalTeachingUnitEntryData(
                resource.Title,
                unitResource.Average,
                unitResource.Coefficient
            );
        }
    }
    
}