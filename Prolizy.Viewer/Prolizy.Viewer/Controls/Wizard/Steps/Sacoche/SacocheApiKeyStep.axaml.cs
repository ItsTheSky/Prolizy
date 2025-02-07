using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prolizy.Viewer.ViewModels.Sacoche;

namespace Prolizy.Viewer.Controls.Wizard.Steps;

public partial class SacocheApiKeyStep : UserControl
{
    public SacocheApiKeyStep(string? apiKey)
    {
        InitializeComponent();
        DataContext = new WizardApiKeyViewModel()
        {
            ApiKey = apiKey
        };
    }
    
    public SacocheApiKeyStep() : this(null) { }
}