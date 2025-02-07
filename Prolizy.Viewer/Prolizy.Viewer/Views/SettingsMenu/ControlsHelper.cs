using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Views.SettingsMenu;

public static class ControlsHelper
{

    public static Control CreateSettingToggleSwitch(string settingProperty,
        string? onContent = null, string? offContent = null,
        Action? onChanged = null)
    {
        onContent ??= "Oui";
        offContent ??= "Non";
        
        var property = typeof(Settings).GetProperty(settingProperty);
        if (property == null)
            throw new ArgumentException($"Property {settingProperty} not found in Settings class.");

        var toggle = new ToggleSwitch
        {
            OnContent = onContent,
            OffContent = offContent,
            HorizontalAlignment = HorizontalAlignment.Center,

            IsChecked = (bool)property.GetValue(Settings.Instance)
        };
        toggle.IsCheckedChanged += (sender, args) =>
        {
            property.SetValue(Settings.Instance, toggle.IsChecked);
            Settings.Instance.Save();
            onChanged?.Invoke();
        };
        return toggle;
    }

    public static Control CreateSettingComboBox<T>(string settingProperty, List<T> choices,
        Func<T, string> displayFunc, Action? onChanged = null)
    {
        var property = typeof(Settings).GetProperty(settingProperty);
        if (property == null)
            throw new ArgumentException($"Property {settingProperty} not found in Settings class.");

        var comboBox = new ComboBox
        {
            ItemsSource = choices.Select(choice => new ComboBoxItem
            {
                Content = displayFunc(choice),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = choice
            }),
            SelectedIndex = choices.IndexOf((T) property.GetValue(Settings.Instance)!),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        comboBox.SelectionChanged += (sender, args) =>
        {
            property.SetValue(Settings.Instance, ((ComboBoxItem) comboBox.SelectedItem!).Tag);
            Settings.Instance.Save();
            onChanged?.Invoke();
        };
        return comboBox;
    }
    
    public static Control CreateSettingButton(string text, ICommand clickCommand, string buttonClass = "accent")
    {
        return new Button
        {
            Classes = { buttonClass },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = text,
            Command = clickCommand
        };
    }
}