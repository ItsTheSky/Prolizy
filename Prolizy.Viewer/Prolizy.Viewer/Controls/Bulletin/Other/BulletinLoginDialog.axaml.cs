using Avalonia.Controls;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Controls.Bulletin.Other;

public partial class BulletinLoginDialog : UserControl
{
    public BulletinLoginDialog()
    {
        InitializeComponent();

        DataContext = new BulletinLoginViewModel();
    }
}