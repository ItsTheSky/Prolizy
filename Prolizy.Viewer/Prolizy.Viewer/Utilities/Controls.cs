using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SpacedGridControl.Avalonia;

namespace Prolizy.Viewer.Utilities;

public static class Controls
{
 
    public static Grid CreateDataGrid(Dictionary<string, string> datas, 
        Color? textColor = null, int rowSpacing = 10, int columnSpacing = 5,
        int[]? separatorIndexes = null)
    {
        separatorIndexes ??= [];

        var rows = datas.Count + separatorIndexes.Length;
        
        string Repeat(string str, int count)
        {
            var result = "";
            for (var i = 0; i < count; i++)
                result += str;
            return result;
        }
        
        var grid = new SpacedGrid
        {
            RowDefinitions = new RowDefinitions(Repeat("Auto,", rows) + "*"),
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowSpacing = rowSpacing,
            ColumnSpacing = columnSpacing
        };
        
        for (int i = 0, index = 0; i < rows; i++)
        {
            if (separatorIndexes.Contains(i))
            {
                var separator = new Separator
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 1
                };
                grid.Children.Add(separator);
                
                Grid.SetRow(separator, index);
                Grid.SetColumnSpan(separator, 2);
            }
            else
            {
                var (key, value) = datas.ElementAt(index - separatorIndexes.Count(x => x < index));
                var entryName = new TextBlock
                {
                    Text = key,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontWeight = FontWeight.SemiBold
                };
                var entryValue = new TextBlock
                {
                    Text = value,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                
                    TextWrapping = TextWrapping.Wrap, MaxWidth = 160
                    //MaxWidth = 150, TextTrimming = TextTrimming.CharacterEllipsis
                };
                ToolTip.SetTip(entryValue, value);
            
                if (textColor != null)
                {
                    entryName.Foreground = new SolidColorBrush(textColor.Value);
                    entryValue.Foreground = new SolidColorBrush(textColor.Value);
                }

                grid.Children.Add(entryName);
                grid.Children.Add(entryValue);
            
                Grid.SetRow(entryName, index);
                Grid.SetColumn(entryName, 0);
                Grid.SetRow(entryValue, index);
                Grid.SetColumn(entryValue, 1);
            }

            index++;
        }
        
        return grid;
    }
    
}