using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyDbShumeiko.Ui.ViewModels;

/// <summary>Рядок відкритої таблиці; <see cref="Index"/> — його номер на сервері.</summary>
public sealed record RowViewModel(int Index, IReadOnlyList<string> Values)
{
    public int Number => Index + 1;
}

/// <summary>Поле введення значення одного поля у формі рядка.</summary>
public sealed partial class CellEditorViewModel(string name, string type) : ObservableObject
{
    public string Label { get; } = $"{name} ({type})";

    [ObservableProperty]
    private string _value = "";
}

/// <summary>Опис поля у формі створення таблиці.</summary>
public sealed partial class NewColumnViewModel(IReadOnlyList<string> types, Action<NewColumnViewModel> remove)
    : ObservableObject
{
    public IReadOnlyList<string> Types { get; } = types;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string? _type = types.FirstOrDefault();

    [RelayCommand]
    private void Remove() => remove(this);
}
