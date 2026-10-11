using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MyDbShumeiko.Client;
using MyDbShumeiko.Ui.ViewModels;
using MyDbShumeiko.Ui.Views;

namespace MyDbShumeiko.Ui;

public partial class App : Application
{
    /// <summary>Адреса сервера за замовчуванням; її задає проєкт запуску (десктоп або веб).</summary>
    public static string DefaultServerUrl { get; set; } = "http://localhost:5218";

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var viewModel = new MainViewModel(new DbApiClient(new HttpClient(), DefaultServerUrl));

        // Десктоп отримує вікно, браузер — єдине подання на всю сторінку.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new MainView { DataContext = viewModel };

        base.OnFrameworkInitializationCompleted();
        viewModel.ConnectCommand.Execute(null);
    }
}
