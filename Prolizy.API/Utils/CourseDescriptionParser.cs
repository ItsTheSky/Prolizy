using System.Text.RegularExpressions;
using System.Web;
using System.Net;

namespace Prolizy.API;

public static class CourseDescriptionParser
{
    private static string DecodeHtmlEntities(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Première passe : Décodage des entités HTML standards
        string decoded = WebUtility.HtmlDecode(input);

        // Deuxième passe : Remplacement des entités HTML numériques spécifiques
        decoded = Regex.Replace(decoded, @"&#(\d+);", match =>
        {
            int number = int.Parse(match.Groups[1].Value);
            return ((char)number).ToString();
        });

        return decoded;
    }

    public static CourseDescription ParseDescription(string description)
    {
        var courseDescription = new CourseDescription();
        
        // Séparation des parties de la description par <br />
        var parts = description.Split(new[] { "<br />", "<br/>", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => DecodeHtmlEntities(p.Trim()))
                             .Where(p => !string.IsNullOrWhiteSpace(p))
                             .ToList();

        int currentIndex = 0;

        // Extraction des professeurs (première partie)
        while (currentIndex < parts.Count && !parts[currentIndex].StartsWith("INF"))
        {
            courseDescription.Professors.Add(parts[currentIndex]);
            currentIndex++;
        }

        // Extraction du groupe (toujours après les professeurs)
        if (currentIndex < parts.Count)
        {
            courseDescription.Groups.Add(parts[currentIndex]);
            currentIndex++;
        }

        // Extraction des salles (toujours après le groupe)
        if (currentIndex < parts.Count)
        {
            var rooms = parts[currentIndex].Split(new[] { "<", "," }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(r => r.Trim())
                                         .Where(r => !string.IsNullOrWhiteSpace(r));
            courseDescription.Rooms.AddRange(rooms);
            currentIndex++;
        }

        // Extraction des matières (dernière partie)
        if (currentIndex < parts.Count)
        {
            courseDescription.Subjects.Add(parts[currentIndex]);
        }

        return courseDescription;
    }
}