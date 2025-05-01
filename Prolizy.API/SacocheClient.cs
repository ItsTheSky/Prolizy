using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Prolizy.API.Model.Sacoche;
using Prolizy.API.Utils;

namespace Prolizy.API;

public class SacocheClient
{

    private string? _sessionId;
    private int _schoolId;

    private string? _apiKey;
    
    public SacocheClient(int schoolId = 294, string? apiKey = null)
    {
        _schoolId = schoolId;
        _apiKey = apiKey;
    }

    #region Login Workflow

    private async Task<string> RetrieveNewSessionId(CancellationToken cancellationToken = default)
    {
        var response = await RequestUtils.GetAsync(RequestUtils.TargetServer.Sacoche, "", null, token: cancellationToken);
        var cookies = response.Headers.GetValues("Set-Cookie");
        var sessionId = cookies.First(c => c.StartsWith("SACoche-session")).Split(";").First();
        if (sessionId == null)
            throw new Exception("Failed to retrieve session ID from SACoche (cookie not found)");
        
        await Task.Delay(2000, cancellationToken);
        return sessionId;
    }
    
    public async Task<string> FetchApiKey(string username, string password)
    {
        _sessionId ??= await RetrieveNewSessionId();
        
        var response = await RequestUtils.PostAsync(RequestUtils.TargetServer.Sacoche, "ajax.php?page=public_accueil&csrf=", new Dictionary<string, string>
        {
            {"f_base", _schoolId.ToString()},
            {"f_login", username},
            {"f_password", password},
            {"f_mode", "normal"},
            {"f_profil", "structure"},
            {"f_action", "identifier"}
        }, RequestUtils.PostFormat.FormUrlEncoded, request =>
        {
            // add back the SACoche-session cookie
            var cookies = new List<string>
            {
                _sessionId,
                "SACoche-is-tactile=0",
                "SACoche-test-cookie=ok"
            };
            
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        });
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        if (!json.RootElement.GetProperty("statut").GetBoolean())
            throw new Exception("Failed to log in: " + WebUtility.HtmlDecode(json.RootElement.GetProperty("value").GetString()));
        
        // Now we need to retrieve the API key
        response = await RequestUtils.GetAsync(RequestUtils.TargetServer.Sacoche, "index.php?page=compte_api", null, request =>
        {
            var cookies = new List<string>
            {
                _sessionId,
                "SACoche-compteur=" + DateTimeOffset.Now.ToUnixTimeSeconds(),
                "SACoche-etablissement=" + _schoolId,
                "SACoche-is-tactile=0",
                "SACoche-mode-connexion=normal",
                "SACoche-test-cookie=ok"
            };
            
            request.Headers.Add("Cookie", string.Join("; ", cookies));
        });

        if (!response.IsSuccessStatusCode)
            throw new Exception("Failed to retrieve API key [" + response.StatusCode + "]: " + await response.Content.ReadAsStringAsync());
        
        content = await response.Content.ReadAsStringAsync();
        var document = new HtmlDocument();
        document.LoadHtml(content);
        var input = document.DocumentNode.SelectSingleNode("//input[@id='f_generer_jeton']");
        if (input == null)
            throw new Exception("Failed to retrieve API key: input not found");
        
        _apiKey = input.GetAttributeValue("value", null);
        if (_apiKey == null)
            throw new Exception("Failed to retrieve API key: empty value");
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            //Console.WriteLine("Failed to retrieve API key: empty value. We have to make the website generates one");
            var csrf = content.Split("CSRF='")[1].Split("'")[0];
            
            response = await RequestUtils.PostAsync(RequestUtils.TargetServer.Sacoche, "ajax.php?page=compte_api",
                new Dictionary<string, string>()
                {
                    {"f_objet", "generer_jeton"},
                    {"csrf", csrf}
                }, RequestUtils.PostFormat.FormUrlEncoded, request =>
                {
                    var cookies = new List<string>
                    {
                        _sessionId,
                        "SACoche-compteur=" + DateTimeOffset.Now.ToUnixTimeSeconds(),
                        "SACoche-etablissement=" + _schoolId,
                        "SACoche-is-tactile=0",
                        "SACoche-mode-connexion=normal",
                        "SACoche-test-cookie=ok"
                    };
                    //Console.WriteLine("Cookies: " + string.Join("; ", cookies));
                    
                    request.Headers.Add("Cookie", string.Join("; ", cookies));
                });

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to generate API key [" + response.StatusCode + "]: " + await response.Content.ReadAsStringAsync());
            
            content = await response.Content.ReadAsStringAsync();
            json = JsonDocument.Parse(content);
            if (!json.RootElement.GetProperty("statut").GetBoolean())
                throw new Exception("Failed to generate API key: " + WebUtility.HtmlDecode(json.RootElement.GetProperty("value").GetString()));
            
            _apiKey = json.RootElement.GetProperty("value").GetString();
        }
        
        //Console.WriteLine("API key: " + _apiKey);
        return _apiKey ?? throw new Exception("API key is null");
    }

    #endregion

    #region API Utils

    private readonly HttpClient _apiClient = new();
    private async Task<T?> GetApi<T>(string service, Dictionary<string, string>? extraData = null)
    {
        if (_apiKey == null)
            throw new Exception("API key is not set");

        var data = new Dictionary<string, string>
        {
            { "service", service },
            { "jeton", _apiKey }
        };
        
        if (extraData != null)
            foreach (var (key, value) in extraData)
                data.Add(key, value);

        var message = new HttpRequestMessage(HttpMethod.Post, new Uri(Constants.BaseSacocheUrl + "api.php"))
        {
            Content = new FormUrlEncodedContent(data)
        };
        var response = await _apiClient.SendAsync(message);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (json.RootElement.GetProperty("code").GetInt32() != 200)
            throw new Exception("Failed to retrieve data from SACoche: " + json.RootElement.GetProperty("message").GetString());

        var rawJson = json.RootElement.GetProperty("data");
        if (rawJson.ValueKind == JsonValueKind.Undefined)
            return default;
        
        return JsonSerializer.Deserialize<T>(json.RootElement.GetProperty("data").GetRawText())!;
    }

    #endregion

    #region API Requests

    public async Task<bool> Logout()
    {
        return await GetApi<object>("logout") == null;
    }

    public async Task<Student?> Login()
    {
        return await GetApi<Student>("login");
    }
    
    public async Task EnsureLogout()
    {
        try
        {
            await Logout();
        }
        catch (Exception e)
        {
            // ignored
        }
    }
    
    public async Task<List<EvaluationReference>?> GetEvaluations(DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var datas = new Dictionary<string, string>();
        if (startDate != null)
            datas.Add("date_debut", startDate.Value.ToString("JJ/MM/AAAA"));
        if (endDate != null)
            datas.Add("date_fin", endDate.Value.ToString("JJ/MM/AAAA"));
        
        return await GetApi<List<EvaluationReference>>("lister_evaluations", datas);
    }

    public async Task<EvaluationSkills> GetSkills(EvaluationReference evaluation)
    {
        var jsonElement = await GetApi<JsonElement>("voir_saisies_evaluation", new Dictionary<string, string>
        {
            { "devoir_id", evaluation.Id.ToString() }
        });
        
        var skills = new EvaluationSkills
        {
            Skills = [],
            SkillNotes = []
        };
        foreach (var property in jsonElement.GetProperty("item").EnumerateObject())
            skills.Skills.Add(jsonElement.GetProperty("item").GetProperty(property.Name).Deserialize<EvaluationSkill>()!);

        var notes = jsonElement.GetProperty("legende").GetProperty("notes");
        foreach (var property in notes.EnumerateObject())
        {
            var skillNote = notes.GetProperty(property.Name).Deserialize<SkillNote>()!;
            if (int.TryParse(property.Name, out var id))
                skills.SkillNotes.Add(id, skillNote);
            else
                skills.SkillNotes.Add(-1, skillNote);
        }

        return skills;
    }

    #endregion
}