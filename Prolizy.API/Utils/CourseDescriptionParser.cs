using System.Net;
using System.Text.RegularExpressions;
using Prolizy.API.Model.Course;

namespace Prolizy.API.Utils;

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
        
        // Traitement de chaque partie
        foreach (var part in parts)
        {
            // Vérification pour les parties multi-informations (séparées par des virgules ou autres délimiteurs)
            if (part.Contains(",") || part.Contains("<"))
            {
                var subParts = part.Split(new[] { ',', '<' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => p.Trim())
                                  .Where(p => !string.IsNullOrWhiteSpace(p));
                
                foreach (var subPart in subParts)
                {
                    ClassifyPart(subPart, courseDescription);
                }
            }
            else
            {
                ClassifyPart(part, courseDescription);
            }
        }
        
        return courseDescription;
    }

    private static void ClassifyPart(string part, CourseDescription courseDescription)
    {
        // Un système de score simple pour déterminer la classification la plus probable
        int professorScore = 0;
        int groupScore = 0;
        int roomScore = 0;
        int subjectScore = 0;
        
        // Indicateurs pour les professeurs
        if (Regex.IsMatch(part, @"[A-Z]{2,}\s+[A-Z][a-z]+")) professorScore += 3; // "NOM Prénom"
        if (Regex.IsMatch(part, @"[A-Z]+-[A-Z]+")) professorScore += 2; // Nom composé avec trait d'union
        if (part.Split(' ').Length >= 3 && part.Split(' ').All(w => w.Length > 0 && char.IsUpper(w[0]))) professorScore += 1; // Plusieurs noms
        
        // Indicateurs pour les groupes
        if (part.StartsWith("INF")) groupScore += 3; // Commence par "INF"
        if (Regex.IsMatch(part, @"INF\d+(-[A-Z])?$")) groupScore += 2; // Format "INF1-B"
        
        // Indicateurs pour les salles
        if (part.EndsWith("VEL")) roomScore += 3; // Se termine par "VEL"
        if (part.Contains(" - VEL")) roomScore += 2; // Contient " - VEL"
        if (Regex.IsMatch(part, @"^\d+\s*-")) roomScore += 1; // Commence par un nombre suivi d'un tiret
        if (Regex.IsMatch(part, @"^[A-Z]\d+\s*-")) roomScore += 1; // Commence par une lettre suivie de chiffres et d'un tiret
        if (Regex.IsMatch(part, @"^Amphi\s+[A-Z]")) roomScore += 2; // Amphi suivi d'une lettre
        if (part.Contains("G2")) roomScore += 1; // Contient G2 (pour G25, etc.)
        
        // Indicateurs pour les matières
        if (Regex.IsMatch(part, @"^R\d+\.\d+")) subjectScore += 3; // Commence par "R" suivi de chiffres et d'un point
        if (Regex.IsMatch(part, @"^SAE\s+\d+\.\d+")) subjectScore += 3; // Commence par "SAE" suivi de chiffres et d'un point
        if (part.Contains("Exploitation") || part.Contains("Communication") || 
            part.Contains("Qualité") || part.Contains("Développement") || 
            part.Contains("Organisation") || part.Contains("Graphes") ||
            part.Contains("Anglais")) subjectScore += 2; // Contient des mots-clés descriptifs
        
        // Détermination du score le plus élevé
        int maxScore = Math.Max(Math.Max(professorScore, groupScore), Math.Max(roomScore, subjectScore));
        
        // Gestion des égalités de score (priorités)
        if (professorScore > 0 && professorScore == groupScore && professorScore == maxScore)
        {
            // Si égalité entre professeur et groupe, privilégier professeur si format typique nom/prénom
            if (Regex.IsMatch(part, @"[A-Z]{2,}\s+[A-Z][a-z]+"))
                professorScore++;
        }
        
        if (roomScore > 0 && roomScore == subjectScore && roomScore == maxScore)
        {
            // Si égalité entre salle et matière, privilégier salle si contient "VEL"
            if (part.Contains("VEL"))
                roomScore++;
        }
        
        // Classification basée sur le score le plus élevé
        if (maxScore == 0)
        {
            // Si aucun score, utilisation des classifications par défaut
            if (part.Contains("VEL"))
            {
                courseDescription.Rooms.Add(part);
            }
            else if (part.StartsWith("INF"))
            {
                courseDescription.Groups.Add(part);
            }
            else if (Regex.IsMatch(part, @"[A-Z]{2,}"))
            {
                courseDescription.Professors.Add(part);
            }
            else
            {
                courseDescription.Subjects.Add(part);
            }
        }
        else if (maxScore == professorScore)
        {
            courseDescription.Professors.Add(part);
        }
        else if (maxScore == groupScore)
        {
            courseDescription.Groups.Add(part);
        }
        else if (maxScore == roomScore)
        {
            courseDescription.Rooms.Add(part);
        }
        else // maxScore == subjectScore
        {
            courseDescription.Subjects.Add(part);
        }
    }
}