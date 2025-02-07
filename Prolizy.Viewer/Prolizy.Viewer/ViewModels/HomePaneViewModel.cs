using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.HomeCards;

namespace Prolizy.Viewer.ViewModels;

public partial class HomePaneViewModel : ObservableObject
{
    
    [ObservableProperty] private ObservableCollection<InternalCard> _cards = new();

    public async Task ReloadCards()
    {
        var enabledCardIds = Settings.Instance.EnabledCards;
        Console.WriteLine($"Reloading cards: {string.Join(", ", enabledCardIds)}");
        var enabledCards = CardManager.CardDefinitions
            .Where(card => enabledCardIds.Contains(card.Id))
            .Select(card =>
            {
                var control = (UserControl)Activator.CreateInstance(card.CardType)!;
                return new InternalCard(control, card);
            })
            .Where(card => card.CardDefinition.IsAvailable())
            .ToList();
        
        Console.WriteLine($"Found enabled cards: {string.Join(", ", enabledCards.Select(card => card.CardDefinition.DisplayName))}");
        
        Cards.Clear();
        foreach (var card in enabledCards)
            Cards.Add(card);
        
        await UpdateCards();
    }

    public async Task UpdateCards(string? specificModule = null)
    {
        foreach (var card in Cards)
            if (specificModule == null || card.CardDefinition.ModuleId == specificModule) 
                await card.CardDefinition.UpdateCard(card.Control);
    }

}

public record InternalCard(UserControl Control, CardDefinition CardDefinition);