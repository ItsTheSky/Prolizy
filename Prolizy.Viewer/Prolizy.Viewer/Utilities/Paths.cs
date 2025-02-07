using System;
using System.Linq;

namespace Prolizy.Viewer.Utilities;

/// <summary>
/// Utility class for file/folder paths.
/// </summary>
public static class Paths
{
    
    /// <summary>
    /// Build a path from a list of strings.
    /// </summary>
    /// <param name="paths">The list of strings to build the path from.</param>
    /// <returns>The built path.</returns>
    public static string Build(params string[] paths)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = System.IO.Path.Combine(folder, "Prolizy");
        
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        return paths.Aggregate(path, System.IO.Path.Combine);
    }
    
}