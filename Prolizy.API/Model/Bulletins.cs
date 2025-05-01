using Prolizy.API.Utils.Converters;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Prolizy.API.Model;

using System.Text.Json.Serialization;

public sealed class BulletinRoot
{
    [JsonPropertyName("config")] public Configuration Config { get; init; }
    [JsonPropertyName("auth")] public Authentication Auth { get; init; }
    [JsonPropertyName("semestres")] public List<Semester> Semesters { get; init; }
    [JsonPropertyName("relevé")] public Transcript Transcript { get; init; }
    [JsonPropertyName("absences")] public Dictionary<DateOnly, List<Absence>> Absences { get; init; }
}

public sealed class Configuration 
{
    [JsonPropertyName("passerelle_version")] public string GatewayVersion { get; init; }
    [JsonPropertyName("histogramme")] public bool ShowHistogram { get; init; }
    [JsonPropertyName("message_non_publication_releve")] public string UnpublishedTranscriptMessage { get; init; }
    [JsonPropertyName("releve_PDF")] public bool EnablePdfTranscript { get; init; }
    [JsonPropertyName("liste_dep_publi_PDF")] public string PdfPublishingDepartments { get; init; }
    [JsonPropertyName("etudiant_modif_photo")] public bool AllowStudentPhotoModification { get; init; }
    [JsonPropertyName("acces_enseignants")] public bool TeacherAccess { get; init; }
    [JsonPropertyName("cloisonner_enseignants")] public bool PartitionTeachers { get; init; }
    [JsonPropertyName("analystics_interne")] public bool InternalAnalytics { get; init; }
    [JsonPropertyName("nom_IUT")] public string InstituteName { get; init; }
    [JsonPropertyName("doc_afficher_nip")] public bool DisplayNip { get; init; }
    [JsonPropertyName("doc_afficher_id")] public bool DisplayId { get; init; }
    [JsonPropertyName("doc_afficher_date_naissance")] public bool DisplayBirthDate { get; init; }
    [JsonPropertyName("module_absences")] public bool EnableAbsenceModule { get; init; }
    [JsonPropertyName("afficher_absences")] public bool ShowAbsences { get; init; }
    [JsonPropertyName("data_absences_scodoc")] public bool UseScodocAbsenceData { get; init; }
    [JsonPropertyName("metrique_absences")] public string AbsenceMetric { get; init; }
    [JsonPropertyName("autoriser_justificatifs")] public bool AllowJustifications { get; init; }
}

public sealed class Authentication 
{
    [JsonPropertyName("session")] public string SessionId { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; }
    [JsonPropertyName("statut")] public int Status { get; init; }
}

public sealed class Semester 
{
    [JsonPropertyName("titre")] public string Title { get; init; }
    [JsonPropertyName("formsemestre_id")] public int SemesterId { get; init; }
    [JsonPropertyName("semestre_id")] public int SemesterNumber { get; init; }
    [JsonPropertyName("annee_scolaire")] public string AcademicYear { get; init; }
}

public sealed class Transcript 
{
    [JsonPropertyName("version")] public string Version { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; }
    [JsonPropertyName("date")] public DateTime Date { get; init; }
    [JsonPropertyName("publie")] public bool IsPublished { get; init; }
    [JsonPropertyName("etat_inscription")] public string EnrollmentStatus { get; init; }
    [JsonPropertyName("etudiant")] public Student Student { get; init; }
    [JsonPropertyName("ressources")] public Dictionary<string, Resource> Resources { get; init; }
    [JsonPropertyName("ues")] public Dictionary<string, TeachingUnit> TeachingUnits { get; init; }
    [JsonPropertyName("saes")] public Dictionary<string, Sae> Saes { get; init; }
    [JsonPropertyName("formsemestre_id")] public int SemesterId { get; init; }
}

public sealed class Sae
{
    
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("titre")] public string Title { get; init; }
    [JsonPropertyName("code_apogee")] public string ApogeeCode { get; init; }
    [JsonPropertyName("url")] public string Url { get; init; }
    [JsonPropertyName("moyenne")] public Grade Average { get; init; }
    [JsonPropertyName("evaluations")] public List<Evaluation> Evaluations { get; init; }
    
}

public sealed class Student 
{
    [JsonPropertyName("civilite")] public string Civility { get; init; }
    [JsonPropertyName("code_nip")] public string StudentId { get; init; }
    [JsonPropertyName("date_naissance")] public string BirthDate { get; init; }
    [JsonPropertyName("dept_acronym")] public string DepartmentAcronym { get; init; }
    [JsonPropertyName("email")] public string Email { get; init; }
    [JsonPropertyName("nom")] public string LastName { get; init; }
    [JsonPropertyName("prenom")] public string FirstName { get; init; }
    [JsonPropertyName("domicile")] public string Address { get; init; }
    [JsonPropertyName("villedomicile")] public string City { get; init; }
    [JsonPropertyName("telephone")] public string Phone { get; init; }
    
    [JsonIgnore] public object FullName => $"{FirstName} {LastName}";
}

public sealed class Resource 
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("titre")] public string Title { get; init; }
    [JsonPropertyName("code_apogee")] public string ApogeeCode { get; init; }
    [JsonPropertyName("moyenne")] public Grade Average { get; init; }
    [JsonPropertyName("evaluations")] public List<Evaluation> Evaluations { get; init; }
}

public sealed class Grade 
{
    [JsonPropertyName("value")] public string Value { get; init; }
    [JsonPropertyName("min")] public string Minimum { get; init; }
    [JsonPropertyName("max")] public string Maximum { get; init; }
    [JsonPropertyName("moy")] public string Average { get; init; }
    
    [JsonPropertyName("rang")] public string Rank { get; init; }
    [JsonPropertyName("total")] public int TotalRank { get; init; }
}

public sealed class Evaluation 
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("coef")] public string Coefficient { get; init; }
    [JsonPropertyName("description")] public string Description { get; init; }
    [JsonPropertyName("date")] public DateTime Date { get; init; }
    [JsonPropertyName("note")] public Grade Grade { get; init; }
    [JsonPropertyName("poids")] public Dictionary<string, int> Weights { get; init; }
    
    public async Task<List<double>?> FetchNotes(BulletinClient client) => await client.FetchListNotes(this);
}

public sealed class TeachingUnit 
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("titre")] public string Title { get; init; }
    [JsonPropertyName("numero")] public int Number { get; init; }
    [JsonPropertyName("type")] public int Type { get; init; }
    [JsonPropertyName("moyenne")] public Grade Average { get; init; }
    
    [JsonPropertyName("ressources")] public Dictionary<string, TeachingUnitEntry> Resources { get; init; }
    [JsonPropertyName("saes")] public Dictionary<string, TeachingUnitEntry> Saes { get; init; }
    
    [JsonPropertyName("bonus")] public string Bonus { get; init; }
    [JsonPropertyName("malus")] public string Malus { get; init; }
    
    [JsonPropertyName("color")] public string Color { get; init; }
}

public sealed class TeachingUnitEntry
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("coef")] public int Coefficient { get; init; }
    [JsonPropertyName("moyenne")] public string Average { get; init; }
}

public sealed class UnitResource 
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("coef")] public int Coefficient { get; init; }
    [JsonPropertyName("moyenne")] public string Average { get; init; }
}

public sealed class Absence 
{
    [JsonPropertyName("idAbs")] public int Id { get; init; }
    [JsonPropertyName("debut"), JsonConverter(typeof(WeirdTimeSpanConverter))] public TimeSpan StartTime { get; init; }
    [JsonPropertyName("fin"), JsonConverter(typeof(WeirdTimeSpanConverter))] public TimeSpan EndTime { get; init; }
    [JsonPropertyName("statut")] public string Status { get; init; }
    [JsonPropertyName("justifie")] public bool IsJustified { get; init; }
    [JsonPropertyName("enseignant")] public string Teacher { get; init; }
    [JsonPropertyName("matiereComplet")] public object Subject { get; init; }
    [JsonPropertyName("dateFin")] public string EndDate { get; init; }
}