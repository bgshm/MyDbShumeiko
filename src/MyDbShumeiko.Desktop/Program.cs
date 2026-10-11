using Avalonia;
using MyDbShumeiko.Ui;

namespace MyDbShumeiko.Desktop;

internal static class Program
{
    // Адресу сервера можна передати першим аргументом командного рядка.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0)
            App.DefaultServerUrl = args[0];
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
