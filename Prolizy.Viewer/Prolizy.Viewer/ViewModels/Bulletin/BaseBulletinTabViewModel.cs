using CommunityToolkit.Mvvm.ComponentModel;

namespace Prolizy.Viewer.ViewModels.Bulletin;

public partial class BaseBulletinTabViewModel(BulletinPaneViewModel baseVm) : ObservableObject
{
    
    public BulletinPaneViewModel BaseViewModel { get; init; } = baseVm;

    public virtual void Update() {}
    
    public virtual void Clear() {}

}