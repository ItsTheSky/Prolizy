using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Prolizy.API;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class ListEvaluationViewModel : SacochePaneBaseViewModel
{
    public override async Task Initialize(SacocheClient client)
    {
        await base.Initialize(client);
        foreach (var eval in Evaluations)
        {
            eval.LoadSkillsCommand = new AsyncRelayCommand(async () => await eval.LoadSkills());
        }
    }
}