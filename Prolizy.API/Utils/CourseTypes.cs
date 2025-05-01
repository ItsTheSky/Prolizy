namespace Prolizy.API.Utils;

/// <summary>
/// Some utility constants that define the type of course.
/// </summary>
public static class CourseTypes
{

    public const string TravauxDiriges = "Travaux Dirigés (TD)";
    public const string TravauxPratiques = "Travaux Pratiques (TP)";
    public const string CoursMagistral = "Cours Magistraux (CM)";
    public const string Examen = "DS";
    public const string JourFerie = "Jour férié";
    public const string Vacances = "Vacances";
    public const string Projet = "Projet en autonomie";
    
    public static bool IsHoliday(this string type)
    {
        return type is JourFerie or Vacances;
    }
    
    public static bool IsExam(this string type)
    {
        return type is Examen;
    }
    
    public static bool IsProject(this string type)
    {
        return type is Projet;
    }

}