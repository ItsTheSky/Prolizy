using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Prolizy.Viewer.Utilities;

/// <summary>
/// Class handling all the settings of the Prolizy app.
/// </summary>
public class Settings : INotifyPropertyChanged
{
    public static bool IsNotAnonymous => !Instance.AnonymousMode;
    public static bool IsDebug => !Instance.Debug;
    
    private static Settings _instance;
    private static readonly object _lock = new ();
    private static readonly string _settingsFilePath = Paths.Build("appsettings.json");

    // Event to notify when a property changes
    public event PropertyChangedEventHandler? PropertyChanged;

    // Core Settings
    private string _defaultModule = "home";
    private string? _studentGroup;
    private string? _sacocheApiKey;
    private bool _debug;
    private bool _isFirstLaunch = true;
    private ObservableCollection<string> _enabledModules = [];
    private ObservableCollection<string> _enabledCards = [];
    private bool _anonymousMode;
    private string themeScheme = "purple";

    // EDT Settings
    private bool _showAsList;
    private bool _caching;
    private bool _betterDescription;
    private int _colorScheme = 3;
    private bool _overlay;
    private WidgetOpenAction _widgetOpenAction = WidgetOpenAction.OpenEdtWithDescription;
    
    // Widget Settings
    private int _widgetUpdateIntervalMinutes = 15;
    private bool _widgetAutoUpdateEnabled = true;
    private bool _widgetSmartUpdateEnabled = false;
    private int _widgetSmartUpdateDelayMinutes = 1;
    
    // Bulletin Settings
    private string _bulletinUsername;
    private string _bulletinPassword;
    private bool _linkEdt = false;

    // Properties with change notification
    public bool AnonymousMode
    {
        get => _anonymousMode;
        set => SetProperty(ref _anonymousMode, value);
    }
    
    public string? StudentGroup
    {
        get => _studentGroup;
        set => SetProperty(ref _studentGroup, value);
    }

    public string? SacocheApiKey
    {
        get => _sacocheApiKey;
        set => SetProperty(ref _sacocheApiKey, value);
    }

    public bool Debug
    {
        get => _debug;
        set => SetProperty(ref _debug, value);
    }
    
    public string ThemeScheme
    {
        get => themeScheme;
        set => SetProperty(ref themeScheme, value);
    }

    public bool ShowAsList
    {
        get => _showAsList;
        set => SetProperty(ref _showAsList, value);
    }
    
    public bool Overlay
    {
        get => _overlay;
        set => SetProperty(ref _overlay, value);
    }

    public bool Caching
    {
        get => _caching;
        set => SetProperty(ref _caching, value);
    }

    public bool BetterDescription
    {
        get => _betterDescription;
        set => SetProperty(ref _betterDescription, value);
    }
    
    public int ColorScheme
    {
        get => _colorScheme;
        set => SetProperty(ref _colorScheme, value);
    }
    
    public string DefaultModule
    {
        get => _defaultModule;
        set => SetProperty(ref _defaultModule, value);
    }
    
    public WidgetOpenAction WidgetOpenAction
    {
        get => _widgetOpenAction;
        set => SetProperty(ref _widgetOpenAction, value);
    }

    public bool IsFirstLaunch
    {
        get => _isFirstLaunch;
        set => SetProperty(ref _isFirstLaunch, value);
    }
    
    public ObservableCollection<string> EnabledModules
    {
        get => _enabledModules;
        set => SetProperty(ref _enabledModules, value);
    }
    
    public ObservableCollection<string> EnabledCards
    {
        get => _enabledCards;
        set => SetProperty(ref _enabledCards, value);
    }
    
    public string BulletinUsername
    {
        get => _bulletinUsername;
        set => SetProperty(ref _bulletinUsername, value);
    }
    
    public string BulletinPassword
    {
        get => _bulletinPassword;
        set => SetProperty(ref _bulletinPassword, value);
    }
    
    public bool LinkEdt
    {
        get => _linkEdt;
        set => SetProperty(ref _linkEdt, value);
    }
    
    // Widget Settings
    public int WidgetUpdateIntervalMinutes
    {
        get => _widgetUpdateIntervalMinutes;
        set => SetProperty(ref _widgetUpdateIntervalMinutes, value);
    }
    
    public bool WidgetAutoUpdateEnabled
    {
        get => _widgetAutoUpdateEnabled;
        set => SetProperty(ref _widgetAutoUpdateEnabled, value);
    }
    
    public bool WidgetSmartUpdateEnabled
    {
        get => _widgetSmartUpdateEnabled;
        set => SetProperty(ref _widgetSmartUpdateEnabled, value);
    }
    
    public int WidgetSmartUpdateDelayMinutes
    {
        get => _widgetSmartUpdateDelayMinutes;
        set => SetProperty(ref _widgetSmartUpdateDelayMinutes, value);
    }

    // Private constructor for singleton
    public Settings()
    {
        
    }

    // Thread-safe Singleton instance
    public static Settings Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= Load();
                }
            }
            return _instance;
        }
    }

    // Load settings
    private static Settings Load()
    {
        Console.WriteLine("Loading settings... from file " + _settingsFilePath);

        Settings SetupSettings(Settings settings)
        {
            settings.EnabledModules = new ObservableCollection<string>(settings.EnabledModules.Distinct());
            settings._enabledModules.CollectionChanged += (sender, args) => settings.OnPropertyChanged(nameof(settings.EnabledModules));
            
            settings.EnabledCards = new ObservableCollection<string>(settings.EnabledCards.Distinct());
            settings._enabledCards.CollectionChanged += (sender, args) => settings.OnPropertyChanged(nameof(settings.EnabledCards));
            
            return settings;
        }
        
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var jsonString = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(jsonString, new JsonSerializerOptions
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                });
                
                return SetupSettings(settings!);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }

        var sett = new Settings();
        sett.Save();
        return SetupSettings(sett);
    }

    // Save settings
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true ,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText(_settingsFilePath, jsonString);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error saving settings: {ex.Message}", ex);
        }
    }

    // Reset to default values
    public void Reset()
    {
        _instance = new Settings();
        Save();
    }

    // Helper method to set property and notify changes
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) 
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    // Method to raise PropertyChanged event
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private const int XorKey = 16334;
}