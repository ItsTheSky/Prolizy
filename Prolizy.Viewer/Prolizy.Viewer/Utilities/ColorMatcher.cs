using Avalonia.Media;

namespace Prolizy.Viewer.Utilities;

using System;
using System.Collections.Generic;

public class ColorMatcher
{
    // Dictionnaire des couleurs Tailwind (weight 400)
    public static readonly Dictionary<string, string> TailwindColors = new()
    {
        { "slate", "#475569" },
        { "gray", "#4b5563" },
        { "zinc", "#52525b" },
        { "neutral", "#525252" },
        { "stone", "#57534e" },
        { "red", "#dc2626" },
        { "orange", "#ea580c" },
        { "amber", "#d97706" },
        { "yellow", "#ca8a04" },
        { "lime", "#65a30d" },
        { "green", "#16a34a" },
        { "emerald", "#059669" },
        { "teal", "#0d9488" },
        { "cyan", "#0891b2" },
        { "sky", "#0284c7" },
        { "blue", "#2563eb" },
        { "indigo", "#4f46e5" },
        { "violet", "#7c3aed" },
        { "purple", "#9333ea" },
        { "fuchsia", "#c026d3" },
        { "pink", "#db2777" },
        { "rose", "#e11d48" }
    };

    #region Constants

    public static Color Slate => Color.Parse(TailwindColors["slate"]);
    public static Color Gray => Color.Parse(TailwindColors["gray"]);
    public static Color Zinc => Color.Parse(TailwindColors["zinc"]);
    public static Color Neutral => Color.Parse(TailwindColors["neutral"]);
    public static Color Stone => Color.Parse(TailwindColors["stone"]);
    public static Color Red => Color.Parse(TailwindColors["red"]);
    public static Color Orange => Color.Parse(TailwindColors["orange"]);
    public static Color Amber => Color.Parse(TailwindColors["amber"]);
    public static Color Yellow => Color.Parse(TailwindColors["yellow"]);
    public static Color Lime => Color.Parse(TailwindColors["lime"]);
    public static Color Green => Color.Parse(TailwindColors["green"]);
    public static Color Emerald => Color.Parse(TailwindColors["emerald"]);
    public static Color Teal => Color.Parse(TailwindColors["teal"]);
    public static Color Cyan => Color.Parse(TailwindColors["cyan"]);
    public static Color Sky => Color.Parse(TailwindColors["sky"]);
    public static Color Blue => Color.Parse(TailwindColors["blue"]);
    public static Color Indigo => Color.Parse(TailwindColors["indigo"]);
    public static Color Violet => Color.Parse(TailwindColors["violet"]);
    public static Color Purple => Color.Parse(TailwindColors["purple"]);
    public static Color Fuchsia => Color.Parse(TailwindColors["fuchsia"]);
    public static Color Pink => Color.Parse(TailwindColors["pink"]);
    public static Color Rose => Color.Parse(TailwindColors["rose"]);
    
    public static IBrush SlateBrush => new SolidColorBrush(Slate);
    public static IBrush GrayBrush => new SolidColorBrush(Gray);
    public static IBrush ZincBrush => new SolidColorBrush(Zinc);
    public static IBrush NeutralBrush => new SolidColorBrush(Neutral);
    public static IBrush StoneBrush => new SolidColorBrush(Stone);
    public static IBrush RedBrush => new SolidColorBrush(Red);
    public static IBrush OrangeBrush => new SolidColorBrush(Orange);
    public static IBrush AmberBrush => new SolidColorBrush(Amber);
    public static IBrush YellowBrush => new SolidColorBrush(Yellow);
    public static IBrush LimeBrush => new SolidColorBrush(Lime);
    public static IBrush GreenBrush => new SolidColorBrush(Green);
    public static IBrush EmeraldBrush => new SolidColorBrush(Emerald);
    public static IBrush TealBrush => new SolidColorBrush(Teal);
    public static IBrush CyanBrush => new SolidColorBrush(Cyan);
    public static IBrush SkyBrush => new SolidColorBrush(Sky);
    public static IBrush BlueBrush => new SolidColorBrush(Blue);
    public static IBrush IndigoBrush => new SolidColorBrush(Indigo);
    public static IBrush VioletBrush => new SolidColorBrush(Violet);
    public static IBrush PurpleBrush => new SolidColorBrush(Purple);
    public static IBrush FuchsiaBrush => new SolidColorBrush(Fuchsia);
    public static IBrush PinkBrush => new SolidColorBrush(Pink);
    public static IBrush RoseBrush => new SolidColorBrush(Rose);
    public static IBrush WhiteBrush => new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

    #endregion

    // Convertit une couleur hex en composantes RGB
    private static (int R, int G, int B) HexToRgb(string hexColor)
    {
        // Enlève le # si présent
        hexColor = hexColor.TrimStart('#');
        
        // Convertit les composantes hex en décimal
        int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
        
        return (r, g, b);
    }

    // Calcule la distance euclidienne entre deux couleurs RGB
    private static double CalculateColorDistance((int R, int G, int B) color1, (int R, int G, int B) color2)
    {
        double deltaR = color1.R - color2.R;
        double deltaG = color1.G - color2.G;
        double deltaB = color1.B - color2.B;
        
        return Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
    }

    // Trouve la couleur Tailwind la plus proche
    public static (string Name, string HexCode) FindClosestTailwindColor(string targetHexColor)
    {
        var targetRgb = HexToRgb(targetHexColor);
        var minDistance = double.MaxValue;
        var closestColor = ("", "");

        foreach (var tailwindColor in TailwindColors)
        {
            var currentRgb = HexToRgb(tailwindColor.Value);
            var distance = CalculateColorDistance(targetRgb, currentRgb);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestColor = (tailwindColor.Key, tailwindColor.Value);
            }
        }

        return closestColor;
    }

    public static Color FindClosestColor(string rawHex)
    {
        var (_, hex) = FindClosestTailwindColor(rawHex);
        return Color.Parse(hex);
    }
}