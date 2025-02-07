using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.Viewer.Controls.Wizard.Steps;

namespace Prolizy.Viewer.Controls.Wizard;

public class WizardManager
{
    public record WizardStep(string Name, Type Type, WizardStep? PreviousStep = null, bool IsNextButtonEnabled = true);

    #region Steps

    public static readonly WizardStep Welcome = new("Bienvenue!", typeof(WelcomeStep));
    public static readonly WizardStep EdtGroup = new("Emploi du Temps", typeof(EdtGroupStep), Welcome, false);
    
    public static readonly WizardStep SacocheChoice = new("SACoche", typeof(SacocheChoiceStep), EdtGroup, false);
    public static readonly WizardStep SacocheLogin = new("SACoche: Connexion Directe", typeof(BulletinLoginDialog), SacocheChoice, false);
    public static readonly WizardStep SacocheApiKey = new("SACoche: Clé d'API", typeof(SacocheApiKeyStep), SacocheChoice, false);
    
    public static readonly WizardStep Finish = new("Terminé!", typeof(FinishStep), SacocheApiKey);
    
    private static readonly List<List<WizardStep>> Steps = new()
    {
        new List<WizardStep> { Welcome, EdtGroup },
        new List<WizardStep> { SacocheChoice, SacocheLogin, SacocheApiKey },
        new List<WizardStep> { Finish }
    };

    #endregion
    
    public static bool HasBeenOpened;
    public static ContentDialog Dialog;
    public static WizardStep CurrentStep;

    private static WizardStep? _nextStep;
    
    public static async Task ShowWizard(bool canClose = false)
    {
        HasBeenOpened = true;
        var frame = new Frame();
        CacheFrames(frame);
        Dialog = new ContentDialog
        {
            Content = frame,
            Theme = Application.Current?.FindResource("WizardContentDialogTheme") as ControlTheme,
            PrimaryButtonText = "Précédent",
            SecondaryButtonText = "Suivant",
        };
        
        if (canClose)
            Dialog.CloseButtonText = "Fermer";
        
        NavigateToStep(Welcome, false);
        CanContinue(EdtGroup);
        
        Dialog.PrimaryButtonClick += (source, args) =>
        {
            args.Cancel = true;
            
            _nextStep = CurrentStep;
            NavigateToStep(CurrentStep.PreviousStep!, true);
        };
        Dialog.SecondaryButtonClick += (source, args) =>
        {
            if (_nextStep == null)
                return;
            
            args.Cancel = true;
            NavigateToStep(_nextStep!, false);
        };
        await Dialog.ShowAsync();
    }

    private static void CacheFrames(Frame frame)
    {
        foreach (var wizardStep in Steps.SelectMany(step => step))
        {
            frame.NavigateToType(wizardStep.Type, null, new FrameNavigationOptions { IsNavigationStackEnabled = false });
        }
    }

    public static void NavigateToStep(WizardStep step, bool isBack)
    {
        CurrentStep = step;
        Dialog.IsPrimaryButtonEnabled = CurrentStep.PreviousStep != null;
        Dialog.IsSecondaryButtonEnabled = step.IsNextButtonEnabled;
        
        if (step == Finish)
        {
            Dialog.IsPrimaryButtonEnabled = false;
            Dialog.IsSecondaryButtonEnabled = false;
            
            Dialog.CloseButtonCommand = new RelayCommand(() => Dialog.Hide());
            Dialog.CloseButtonText = "Fermer";
        }
        
        var navOpt = new FrameNavigationOptions() { TransitionInfoOverride = new EntranceNavigationTransitionInfo
        {
            FromHorizontalOffset = isBack ? -100 : 100,
            FromVerticalOffset = 0
        } };
        
        UpdateTitle();

        var frame = Dialog.Content as Frame;
        frame?.NavigateToType(step.Type, null, navOpt);
        try
        {
            step.Type.GetMethod("Refresh")?.Invoke(null, null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public static void CanContinue(WizardStep nextStep)
    {
        _nextStep = nextStep;
        Dialog.IsSecondaryButtonEnabled = true;
    }
    
    public static void CannotContinue()
    {
        Dialog.IsSecondaryButtonEnabled = false;
    }

    public static void UpdateTitle()
    {
        var maxIndex = Steps.Count;
        var currentIndex = 0;
        for (var i = 0; i < Steps.Count; i++)
        {
            if (!Steps[i].Contains(CurrentStep)) 
                continue;
            currentIndex = i;
            break;
        }

        Dialog.Title = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = $"{currentIndex + 1}/{maxIndex}",
                    FontSize = 20,
                    FontWeight = FontWeight.Light,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new TextBlock
                {
                    Text = CurrentStep.Name,
                    FontSize = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                }
            },
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

}