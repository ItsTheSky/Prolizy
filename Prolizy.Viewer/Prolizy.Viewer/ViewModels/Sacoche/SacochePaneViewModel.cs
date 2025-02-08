using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.API;
using Prolizy.Viewer.Controls.Wizard.Steps;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class SacochePaneViewModel : ObservableObject
{
    public SacochePaneViewModel()
    {
        ListViewModel = new ListEvaluationViewModel();
        ReleveViewModel = new ReleveEvaluationViewModel();
        GraphiqueViewModel = new GraphiqueEvaluationViewModel();
    }

    [ObservableProperty] private bool _isSacocheAvailable;
    [ObservableProperty] private Student? _student;
    [ObservableProperty] private bool _isLoading;

    public ListEvaluationViewModel ListViewModel { get; }
    public ReleveEvaluationViewModel ReleveViewModel { get; }
    public GraphiqueEvaluationViewModel GraphiqueViewModel { get; }

    private bool _hasTriedToLogin;

    public async Task RefreshState()
    {
        _student = null;
        IsSacocheAvailable = !string.IsNullOrEmpty(Settings.Instance.SacocheApiKey);
        
        if (IsSacocheAvailable)
            await RefreshEvals();
    }

    [RelayCommand]
    public async Task RefreshEvals()
    {
        IsLoading = true;
        try
        {
            if (!IsSacocheAvailable) return;

            var client = new SacocheClient(apiKey: Settings.Instance.SacocheApiKey);
            try
            {
                await client.Logout();
            }
            catch (Exception)
            {
                // ignored
            }

            await ListViewModel.Initialize(client);
            await ReleveViewModel.Initialize(client);
            await GraphiqueViewModel.Initialize(client);
            
            Student = ListViewModel.Student;
        }
        catch (Exception e)
        {
            if (!_hasTriedToLogin)
            {
                _hasTriedToLogin = true;
                await RefreshEvals();
            }
            else
            {
                await Dialogs.ShowMessage("Erreur", e.ToString());
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public static AsyncRelayCommand ConfigureSacocheCommand => new (async () =>
    {
        var frame = new Frame()
        {
            IsNavigationStackEnabled = true
        };
        var dialog = new ContentDialog
        {
            Content = frame,
            Title = "Configuration de Sacoche",

            PrimaryButtonText = "Continuer",
            CloseButtonText = "Fermer",
            IsPrimaryButtonEnabled = false
        };
        dialog.CloseButtonCommand = new RelayCommand(() => dialog.Hide(ContentDialogResult.Secondary));
        dialog.Closing += (source, args) =>
        {
            if (args.Result != ContentDialogResult.Secondary)
                args.Cancel = true;
        };
        //dialog.PrimaryButtonClick += (source, args) => args. = true;
        void Next()
        {
            var type = (SacocheChoiceStep.Instance.DirectLoginRadio.IsChecked ?? false)
                ? typeof(SacocheLoginStep)
                : typeof(SacocheApiKeyStep);

            frame.NavigateToType(type, null, new FrameNavigationOptions
            {
                TransitionInfoOverride = new SlideNavigationTransitionInfo
                {
                    Effect = SlideNavigationTransitionEffect.FromRight
                },
                IsNavigationStackEnabled = true
            });
            dialog.PrimaryButtonText = "Retour";
            dialog.IsPrimaryButtonEnabled = true;
            dialog.PrimaryButtonCommand = new RelayCommand(Previous);
        }
        void Previous()
        {
            frame.GoBack();
            dialog.PrimaryButtonText = "Continuer";
            dialog.PrimaryButtonCommand = new RelayCommand(Next);
        }
        dialog.PrimaryButtonCommand = new RelayCommand(Next);
        
        frame.NavigateToType(typeof(SacocheChoiceStep), null, new FrameNavigationOptions
        {
            TransitionInfoOverride = new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            },
            IsNavigationStackEnabled = true
        });
        SacocheChoiceStep.Instance.CurrentDialog = dialog;
        
        await dialog.ShowAsync();
    });
}