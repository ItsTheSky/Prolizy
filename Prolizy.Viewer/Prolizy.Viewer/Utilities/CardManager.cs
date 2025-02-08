using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentIcons.Common;
using Prolizy.Viewer.Cards;

namespace Prolizy.Viewer.Utilities;

/// <summary>
/// The manager class for the home page cards.
///
/// This class will handle the organization, update and
/// serialization/deserialization of the cards within Prolizy.
/// </summary>
public static class CardManager
{

    #region Available Cards

    public static readonly List<CardDefinition> CardDefinitions =
    [
        new EdtCardDefinition()
    ];

    #endregion

}

public abstract class CardDefinition
{
    public abstract Type CardType { get; }
    public abstract string Id { get; }
    
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract Symbol Symbol { get; }
    public abstract string ModuleId { get; }
    
    public abstract bool IsAvailable();
    public abstract Task UpdateCard(object card);
    
}