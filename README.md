# MyDbShumeiko

Часткова реалізація системи управління табличними базами даних: REST API на .NET 9
і два клієнти до нього — десктопний та вебклієнт — зі спільним інтерфейсом на Avalonia UI.

## Можливості

- створення бази, збереження її на диск сервера та зчитування з диска;
- створення таблиць із валідацією схеми та їх знищення;
- додавання, перегляд, редагування та знищення рядків із валідацією значень;
- типи полів: `integer`, `real`, `char`, `string`, `color` (RGB-код `#RRGGBB`),
  `colorInvl` (інтервал кольорів `#RRGGBB-#RRGGBB`);
- прямий (декартів) добуток двох таблиць.

## Структура

| Проєкт | Призначення |
|---|---|
| `src/MyDbShumeiko.Core` | Ядро: бази, таблиці, типи, валідація, прямий добуток, збереження |
| `src/MyDbShumeiko.Contracts` | Об'єкти обміну між сервером і клієнтами |
| `src/MyDbShumeiko.Api` | REST API (ASP.NET Core 9), документація Swagger на `/swagger` |
| `src/MyDbShumeiko.Client` | Типізований HTTP-клієнт |
| `src/MyDbShumeiko.Ui` | Спільний інтерфейс: XAML-подання та ViewModel |
| `src/MyDbShumeiko.Desktop` | Десктоп-клієнт |
| `src/MyDbShumeiko.Web` | Вебклієнт (WebAssembly) |
| `tests/MyDbShumeiko.Tests` | Тести (xUnit) |

## Запуск

Потрібен .NET 9 SDK; для вебклієнта додатково `dotnet workload install wasm-tools`.

Сервер:

```bash
dotnet run --project src/MyDbShumeiko.Api --launch-profile http
```

Десктоп-клієнт:

```bash
dotnet run --project src/MyDbShumeiko.Desktop
```

Сервер разом із вебклієнтом з однієї адреси:

```bash
powershell -ExecutionPolicy Bypass -File publish.ps1
dotnet publish/MyDbShumeiko.Api.dll --urls http://localhost:8080
```

Після цього вебверсія доступна на `http://localhost:8080`.

## Тести

```bash
dotnet test
```

## Документація

- UML-діаграми: [docs/uml/MyDbShumeiko.drawio](docs/uml/MyDbShumeiko.drawio)
- Розгортання на Amazon EC2: [docs/deploy-aws.md](docs/deploy-aws.md)
