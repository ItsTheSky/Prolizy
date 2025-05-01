using Avalonia.Controls;
using Prolizy.Viewer.ViewModels.SacocheWizard;

namespace Prolizy.Viewer.Controls.Wizard.Steps.Sacoche;

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