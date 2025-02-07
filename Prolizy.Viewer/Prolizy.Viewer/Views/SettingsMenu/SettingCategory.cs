using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Views.SettingsMenu;

/// <summary>
/// Represent a category/sub-page in the settings menu. 
/// </summary>
public abstract class SettingCategory : ObservableObject
{
    
    /// <summary>
    /// The displayed Title of this category.
    /// </summary>
    public abstract string Title { get; }
    
    /// <summary>
    /// Whether this category is a module or not.
    /// If this is true, an activation card will be displayed to enable/disable the module.
    /// </summary>
    public bool IsModule => ModuleId != null;
    
    public abstract string? ModuleId { get; }
    
    /// <summary>
    /// The entries in this category.
    /// </summary>
    public abstract List<SettingEntry> Entries { get; }
    
    /// <summary>
    /// The custom control to display in the category.
    /// If this is specified, the entries will be ignored.
    /// </summary>
    public virtual Control? CustomControl { get; }

    public virtual void OnModuleStateChanged(bool state)
    {
        OnPropertyChanged(nameof(IsModuleEnabled));
        OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(CustomControl));
    }

    public bool IsModuleEnabled
    {
        get => Settings.Instance.EnabledModules.Contains(ModuleId);
        set
        {
            if (value)
                Settings.Instance.EnabledModules.Add(ModuleId);
            else
                Settings.Instance.EnabledModules.Remove(ModuleId);
            Settings.Instance.Save();
            
            OnPropertyChanged();
            OnModuleStateChanged(value);
        }
    }
}

/// <summary>
/// Represent a single setting entry in a category.
/// </summary>
public partial class SettingEntry(SettingCategory category, string id) : ObservableObject
{
    public SettingCategory Category => category;
    public string Id => id;
    
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private bool _isVisible = true;

    [ObservableProperty] private Control _control;
}