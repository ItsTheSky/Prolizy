using System.Text.Json;

namespace Prolizy.API;

public static class EDTClient
{
    
    public static async Task<List<Course>> GetCourses(CalendarRequest request, CancellationToken token = default)
    {
        var response = await RequestUtils.PostAsync(RequestUtils.TargetServer.Edt, 
            "GetCalendarData", request, token: token);
        var content = await response.Content.ReadAsStringAsync(token);
        return JsonUtils.Deserialize<List<Course>>(content);
    }
    
    public static async Task<CourseDescription> GetDescription(this Course course, bool offline = false, CancellationToken token = default)
    {
        if (offline)
            return CourseDescriptionParser.ParseDescription(course.RawDescription);
        
        var obj = new { eventId = course.Id };
        var response = await RequestUtils.PostAsync(RequestUtils.TargetServer.Edt, "GetSideBarEvent", obj, token: token);
        var content = await response.Content.ReadAsStringAsync(token);
        var elementsJson = JsonDocument.Parse(content).RootElement.GetProperty("elements");
        var description = new CourseDescription();

        string? lastElementType = null;
        foreach (var element in elementsJson.EnumerateArray())
        {
            var key = element.GetProperty("label").GetString();
            if (key == null && lastElementType != null)
                key = lastElementType;
            else
                lastElementType = key;
            
            var value = (element.GetProperty("content").ValueKind == JsonValueKind.Array 
                ? string.Join(", ", element.GetProperty("content").EnumerateArray().Select(e => e.GetString())) 
                : element.GetProperty("content").GetString())
                ?.Replace("<br />", "\n");
            if (value == null)
                continue;
            
            switch (key)
            {
                case "Personnel":
                    description.Professors.Add(value);
                    break;
                case "Salle":
                    description.Rooms.Add(value);
                    break;
                case "Matière":
                case "Matières":
                    description.Subjects.Add(value);
                    break;
                case "Groupe":
                    description.Groups.Add(value);
                    break;
                case "Catégorie d’événement":
                    description.EventTypes.Add(value);
                    break;
                case "Remarques":
                    description.Notes.Add(value);
                    break;
                case "Heure":
                    description.Hours.Add(value);
                    break;
            }
        }
        
        foreach (var prop in typeof(CourseDescription).GetProperties())
        {
            if (prop.PropertyType == typeof(List<string>))
            {
                var list = (List<string>) prop.GetValue(description)!;
                if (list.Count == 0)
                    list.Add("Inconnu");
            }
        }
        
        return description;
    }
    
    public static async Task<List<string>> GetGroups(FederationRequest request, CancellationToken token = default)
    {
        var response = await RequestUtils.PostAsync(RequestUtils.TargetServer.Edt, "ReadResourceListItems", request, token: token);
        var content = await response.Content.ReadAsStringAsync(token);
        //Console.WriteLine(content);
        var json = JsonDocument.Parse(content);
        var results = json.RootElement.GetProperty("results");
        var groups = new List<string>();
        foreach (var group in results.EnumerateArray())
            groups.Add(group.GetProperty("text").GetString()!);
        return groups;
    }
    
}