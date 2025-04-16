using CommunityToolkit.Mvvm.Input;

namespace Prolizy.Viewer.ViewModels.Bulletin;

public partial class BulletinUnitsViewModel(BulletinPaneViewModel vm) : BaseBulletinTabViewModel(vm)
{

    private bool isExpanded = true;
    [RelayCommand]
    public void SpreadClicked()
    {
        foreach (var teachingUnit in vm.Units)
            teachingUnit.IsExpanded = !isExpanded;
        isExpanded = !isExpanded;
    }
    
}