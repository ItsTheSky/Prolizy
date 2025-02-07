using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Prolizy.API;

public static class RequestUtils
{
    public enum Method { Get, Post }
    public enum TargetServer { Edt, Bulletin, Sacoche }
    public enum PostFormat { Json, FormUrlEncoded }
    
    private static HttpClient _client = new();
    
    public static void SetHttpClient(HttpClient client)
    {
        _client = client;
    }

    private static string GetServerUrl(TargetServer targetServer)
    {
        return targetServer switch
        {
            TargetServer.Edt => Constants.BaseEdtUrl,
            TargetServer.Bulletin => Constants.BaseBulletinUrl,
            TargetServer.Sacoche => Constants.BaseSacocheUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(targetServer), targetServer, null)
        };
    }

    /// <summary>
    /// Délégué permettant de personnaliser la requête HTTP juste avant son envoi
    /// </summary>
    public delegate void RequestCustomizer(HttpRequestMessage request);
    
    public static async Task<HttpResponseMessage> SendAsync(
        Method method,
        TargetServer targetServer,
        string endpoint,
        object? data,
        PostFormat postFormat = PostFormat.FormUrlEncoded,
        RequestCustomizer customizer = null,
        CancellationToken token = default
    )
    {
        var request = new HttpRequestMessage(method switch
        {
            Method.Get => HttpMethod.Get,
            Method.Post => HttpMethod.Post,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        }, GetServerUrl(targetServer) + endpoint);

        return await SendAsync(request, targetServer, data, postFormat, customizer, token);
    }

    public static async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        TargetServer targetServer,
        object? data,
        PostFormat postFormat = PostFormat.FormUrlEncoded,
        RequestCustomizer customizer = null,
        CancellationToken token = default
    )
    {
        #region Base Headers

        switch (targetServer)
        {
            case TargetServer.Edt:
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.01));
        
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
        
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr-FR", 1));
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr", 0.8));   
                break;
        }

        #endregion

        #region Data Handling

        if (data is not null)
        {
            if (request.Method == HttpMethod.Post)
            {
                switch (postFormat)
                {
                    case PostFormat.Json:
                        var jsonContent = JsonSerializer.Serialize(data);
                        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        break;
                
                    case PostFormat.FormUrlEncoded:
                        var parameters = JsonUtils.ToDictionary(data);
                        request.Content = new FormUrlEncodedContent(parameters);
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        break;
                
                    default:
                        throw new ArgumentOutOfRangeException(nameof(postFormat), postFormat, null);
                }
            }
            else
            {
                var query = new StringBuilder();
                foreach (var (key, value) in JsonUtils.ToDictionary(data))
                {
                    query.Append($"{key}={value}&");
                }
                request.RequestUri = new Uri(request.RequestUri + "?" + query.ToString().TrimEnd('&'));
            }
        }

        #endregion

        // Application des personnalisations de requête si spécifiées
        customizer?.Invoke(request);
        
        return await _client.SendAsync(request, token);
    }

    #region Utility Methods

    public static async Task<HttpResponseMessage> GetAsync(
        TargetServer targetServer,
        string endpoint,
        object? parameters,
        RequestCustomizer customizer = null,
        CancellationToken token = default)
    {
        return await SendAsync(Method.Get, targetServer, endpoint, parameters, PostFormat.FormUrlEncoded, customizer, token);
    }
    
    public static async Task<HttpResponseMessage> PostAsync(
        TargetServer targetServer,
        string endpoint,
        object? content,
        PostFormat postFormat = PostFormat.FormUrlEncoded,
        RequestCustomizer customizer = null,
        CancellationToken token = default)
    {
        return await SendAsync(Method.Post, targetServer, endpoint, content, postFormat, customizer, token);
    }

    #endregion

    public static HttpClient GetHttpClient()
    {
        return _client;
    }
}