using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Prolizy.Viewer;

[assembly: SupportedOSPlatform("browser")]
internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        /*var builder = WebAssemblyHostBuilder.CreateDefault(args);
        
        // Ajouter la configuration CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        */

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}