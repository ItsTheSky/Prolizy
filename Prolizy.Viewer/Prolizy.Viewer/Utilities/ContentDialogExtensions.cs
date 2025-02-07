using System;
using System.Reflection;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace Prolizy.Viewer.Utilities;

public static class ContentDialogExtensions
{
    
    public enum ButtonType
    {
        Primary,
        Secondary,
        Close
    }
    
    public static void SetButtonClasses(this ContentDialog dialog, 
        ButtonType buttonType, 
        params string[] classes)
    {
        var button = dialog.GetButton(buttonType);
        button.Classes.AddRange(classes);
    }
    
    private static Button GetButton(this ContentDialog dialog, ButtonType buttonType)
    {
        var fieldName =  buttonType switch
        {
            ButtonType.Primary => "_primaryButton",
            ButtonType.Secondary => "_secondaryButton",
            ButtonType.Close => "_closeButton",
            _ => throw new ArgumentOutOfRangeException(nameof(buttonType), buttonType, null)
        };
        
        return (Button) dialog.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(dialog)!;
    }
    
}