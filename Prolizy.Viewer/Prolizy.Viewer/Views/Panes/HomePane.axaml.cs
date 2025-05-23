﻿using System.Threading.Tasks;
using Avalonia.Controls;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Views.Panes;

public partial class HomePane : UserControl
{
    public static HomePane Instance { get; private set; }
    
    public HomePane()
    {
        InitializeComponent();
        
        Instance = this;
        DataContext = new HomePaneViewModel();
    }

    public HomePaneViewModel ViewModel => ((HomePaneViewModel) DataContext)!;

    public static async Task UpdateCards(string? module = null)
    {
        if (Instance == null! || Instance.ViewModel == null!)
            return;
        
        await Instance.ViewModel.UpdateCards(module);
    }
}