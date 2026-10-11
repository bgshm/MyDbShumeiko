using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using MyDbShumeiko.Ui;

[assembly: SupportedOSPlatform("browser")]

namespace MyDbShumeiko.Web;

internal static class Program
{
    // main.js передає першим аргументом адресу сторінки.
    private static Task Main(string[] args)
    {
        if (args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var page))
            App.DefaultServerUrl = ServerUrl(page);

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();

    /// <summary>
    /// Сервер — той, що віддав сторінку; під час розробки, коли клієнт і API
    /// працюють на різних портах, його задає параметр <c>?server=</c>.
    /// </summary>
    private static string ServerUrl(Uri page)
    {
        foreach (var pair in page.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "server")
                return Uri.UnescapeDataString(parts[1]);
        }

        return page.GetLeftPart(UriPartial.Authority);
    }
}
