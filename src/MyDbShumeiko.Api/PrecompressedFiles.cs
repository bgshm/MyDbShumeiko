using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace MyDbShumeiko.Api;

/// <summary>
/// Роздача статичних файлів вебклієнта зі стисненням: публікація кладе поруч із кожним файлом
/// готові копії <c>.br</c> і <c>.gz</c>, і якщо браузер приймає таке кодування, віддаємо копію.
/// </summary>
public static class PrecompressedFiles
{
    // Brotli першим: він стискає краще, але браузери просять його лише через HTTPS.
    private static readonly (string Extension, string Encoding)[] Variants = [(".br", "br"), (".gz", "gzip")];

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static IApplicationBuilder UsePrecompressedStaticFiles(this WebApplication app)
    {
        var files = app.Environment.WebRootFileProvider;

        app.Use((context, next) =>
        {
            var request = context.Request;
            if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) && request.Path.HasValue)
            {
                var accepted = request.Headers.AcceptEncoding.ToString()
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Split(';')[0].Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var (extension, encoding) in Variants)
                {
                    if (accepted.Contains(encoding) && files.GetFileInfo(request.Path.Value + extension).Exists)
                    {
                        request.Path = request.Path.Value + extension;
                        break;
                    }
                }
            }

            return next();
        });

        return app.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            OnPrepareResponse = context =>
            {
                var name = context.File.Name;
                var variant = Variants.FirstOrDefault(v => name.EndsWith(v.Extension, StringComparison.OrdinalIgnoreCase));
                if (variant.Extension is null)
                    return;

                // Тип вмісту беремо від початкового файла, а не від стиснутої копії.
                var original = name[..^variant.Extension.Length];
                var headers = context.Context.Response.Headers;
                headers[HeaderNames.ContentEncoding] = variant.Encoding;
                headers[HeaderNames.ContentType] =
                    ContentTypes.TryGetContentType(original, out var type) ? type : "application/octet-stream";
                headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);
            },
        });
    }
}
