using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;

namespace Prolizy.Viewer.Utilities;

public static class Dialogs
{

    public static async Task ShowMessage(string title, object content,
        [CallerMemberName] string caller = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = content,
            
            CloseButtonText = "Fermer"
        };
        
        Console.WriteLine($"[{caller}] {title}: {content}");
        
        await dialog.ShowAsync();
    }
    
    public static async Task AskChoice(string title, object content, string choice1, string choice2,
        Action<bool> onChoice,
        bool hasCancel = true,
        [CallerMemberName] string caller = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = content,
            PrimaryButtonText = choice1,
            SecondaryButtonText = choice2
        };
        
        if (hasCancel)
            dialog.CloseButtonText = "Annuler";
        
        Console.WriteLine($"[{caller}] {title}: {content}");
        
        var result = await dialog.ShowAsync();
        
        onChoice(result == ContentDialogResult.Primary);
    }
     
}