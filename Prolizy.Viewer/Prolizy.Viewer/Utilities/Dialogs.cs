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
     
}