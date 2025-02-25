using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Prolizy.API.Model;

namespace Prolizy.API;

public class BulletinClient
{

    public bool IsLoggedIn { get; private set; }
    private readonly HttpClient _client = new ();
    public string Username { get; init; }
    public string Password { get; init; }

    public async Task<HttpStatusCode> Login()
    {
        string url;
        HttpResponseMessage response;
        
        // 1. Gather the cookies
        url = "https://bulletins.iut-velizy.uvsq.fr/services/data.php?q=dataPremi%C3%A8reConnexion";
        response = await _client.PostAsync(url, null);
        
        // 2. Gather JWT token
        url = "https://cas2.uvsq.fr/cas/login?service=https%3A%2F%2Fbulletins.iut-velizy.uvsq.fr%2Fservices%2FdoAuth.php%3Fhref%3Dhttps%253A%252F%252Fbulletins.iut-velizy.uvsq.fr%252F";
        response = await _client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var token = doc.DocumentNode.SelectSingleNode("//input[@name='execution']")
            .GetAttributeValue("value", "");
        
        // 3. Login
        url = "https://cas2.uvsq.fr/cas/login?service=https%3A%2F%2Fbulletins.iut-velizy.uvsq.fr%2Fservices%2FdoAuth.php%3Fhref%3Dhttps%253A%252F%252Fbulletins.iut-velizy.uvsq.fr%252F";
        var dict = new Dictionary<string, string>()
        {
            { "username", Username },
            { "password", Password },
            { "execution", token },
            { "_eventId", "submit" },
            { "geolocation", "" }
        };
        var content = new FormUrlEncodedContent(dict);
        response = await _client.PostAsync(url, content);
        
        if (response.StatusCode == HttpStatusCode.OK)
            IsLoggedIn = true;
        
        return response.StatusCode;
    }

    /// <summary>
    /// Fetch the bulletin data from the API.
    /// - If a semesterId is provided, fetch the data for that semester ID. In this case,
    /// the returned object will only have two fields: `relevé` and `absences` (transcript and absences).
    /// - If no semesterId is provided, fetch the data for the first connection. In this case,
    /// all data (including semesters) will be returned.
    /// </summary>
    /// <param name="semesterId">The desired semester ID, that can be got through <see cref="Semester"/> class.</param>
    /// <returns>The bulletin data.</returns>
    public async Task<BulletinRoot?> FetchDatas(int semesterId = -1)
    {
        var url = semesterId == -1 
            ? "https://bulletins.iut-velizy.uvsq.fr/services/data.php?q=dataPremi%C3%A8reConnexion" 
            : $"https://bulletins.iut-velizy.uvsq.fr/services/data.php?q=relev%C3%A9Etudiant&semestre={semesterId}";
        
        var response = await _client.PostAsync(url, null);
        var json = (await response.Content.ReadAsStringAsync()).ReplaceLineEndings("")
            .Replace("\"absences\":[]", "\"absences\":{}"); // Fixing the JSON format
        
        return JsonSerializer.Deserialize<BulletinRoot>(json);
    }

    /// <inheritdoc cref="FetchListNotes(int)"/>
    public async Task<List<double>?> FetchListNotes(Evaluation evaluation)
    {
        return await FetchListNotes(evaluation.Id);
    }

    /// <summary>
    /// Fetch the notes list for a given evaluation. This will return
    /// an array of doubles, each representing a note.
    /// </summary>
    /// <param name="evaluationId">The evaluation ID.</param>
    /// <returns>The notes list.</returns>
    public async Task<List<double>?> FetchListNotes(int evaluationId)
    {
        var url = $"https://bulletins.iut-velizy.uvsq.fr/services/data.php?q=listeNotes&eval={evaluationId}";
        var response = await _client.PostAsync(url, null);
        var json = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<List<double>>(json)!;
        }
        catch (Exception e)
        {
            return null;
        }
    }

}