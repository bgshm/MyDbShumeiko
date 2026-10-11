using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using MyDbShumeiko.Contracts;
using MyDbShumeiko.Ui.ViewModels;

namespace MyDbShumeiko.Ui.Views;

public partial class MainView : UserControl
{
    private MainViewModel? _viewModel;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as MainViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        RebuildColumns();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Columns))
            RebuildColumns();
    }

    // Набір полів відомий лише під час виконання, тому стовпці сітки будуються кодом.
    private void RebuildColumns()
    {
        RowsGrid.Columns.Clear();
        var columns = _viewModel?.Columns ?? [];
        if (columns.Count == 0)
            return;

        RowsGrid.Columns.Add(CreateColumn("№", row => new TextBlock { Text = row.Number.ToString(), Opacity = 0.6 }));
        for (var i = 0; i < columns.Count; i++)
        {
            var index = i;
            var column = columns[i];
            RowsGrid.Columns.Add(CreateColumn(
                $"{column.Name} ({column.Type})",
                row => CreateCell(column, index < row.Values.Count ? row.Values[index] : "")));
        }
    }

    private static DataGridTemplateColumn CreateColumn(string header, Func<RowViewModel, Control> content)
    {
        return new DataGridTemplateColumn
        {
            Header = header,
            CellTemplate = new FuncDataTemplate<RowViewModel>((_, _) =>
            {
                // Сітка перевикористовує клітинки для інших рядків, тому вміст
                // оновлюємо при кожній зміні DataContext, а не один раз при створенні.
                var cell = new ContentControl { Margin = new Thickness(8, 4), VerticalAlignment = VerticalAlignment.Center };
                cell.DataContextChanged += (_, _) =>
                    cell.Content = cell.DataContext is RowViewModel row ? content(row) : null;
                return cell;
            }),
        };
    }

    private static Control CreateCell(ColumnDto column, string value)
    {
        var text = new TextBlock { Text = value, VerticalAlignment = VerticalAlignment.Center };
        var brush = column.Type switch
        {
            "color" => ColorBrush(value),
            "colorInvl" => IntervalBrush(value),
            _ => null,
        };
        if (brush is null)
            return text;

        // Для кольорових типів поруч зі значенням показуємо зразок кольору.
        var swatch = new Border
        {
            Width = column.Type == "color" ? 16 : 40,
            Height = 16,
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            Background = brush,
        };
        return new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { swatch, text } };
    }

    private static IBrush? ColorBrush(string value) =>
        Color.TryParse(value, out var color) ? new SolidColorBrush(color) : null;

    private static IBrush? IntervalBrush(string value)
    {
        var parts = value.Split('-');
        if (parts.Length != 2 || !Color.TryParse(parts[0], out var from) || !Color.TryParse(parts[1], out var to))
            return null;

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = { new GradientStop(from, 0), new GradientStop(to, 1) },
        };
    }
}
