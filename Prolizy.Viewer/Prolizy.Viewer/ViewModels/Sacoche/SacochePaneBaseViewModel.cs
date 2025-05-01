using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API;
using Prolizy.API.Model.Sacoche;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class SacochePaneBaseViewModel : ObservableObject
{
    protected SacocheClient SacocheClient { get; private set; } = null!;
    
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<InternalEvaluationReference> _evaluations = new();
    
    public virtual async Task Initialize(SacocheClient client)
    {
        SacocheClient = client;
        IsLoading = true;
        try 
        {
            await SacocheClient.EnsureLogout();
            Student = await SacocheClient.Login();
            
            var evals = await SacocheClient.GetEvaluations() ?? [];
            Evaluations = new ObservableCollection<InternalEvaluationReference>(
                evals.Select(e => new InternalEvaluationReference { 
                    SacocheClient = SacocheClient, 
                    Evaluation = e 
                })
            );
        }
        finally
        {
            IsLoading = false;
        }
    }
}