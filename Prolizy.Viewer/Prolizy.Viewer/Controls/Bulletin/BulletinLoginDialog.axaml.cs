using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.ViewModels.Sacoche;

namespace Prolizy.Viewer.Controls.Wizard.Steps;

public partial class BulletinLoginDialog : UserControl
{
    public BulletinLoginDialog()
    {
        InitializeComponent();

        DataContext = new BulletinLoginViewModel();
    }
}