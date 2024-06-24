using System.Collections;
using System.Text.Json;

namespace Core.IO;

public class DocumentReader<TResult> : IEnumerable<TResult>, IReadOnlyCollection<TResult>
{
    private static JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IEnumerable<string> files;

    public DocumentReader(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException("You should provide a valid directory path");

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory {path} does not exists.");

        files = Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories);
    }

    public int Count => files.Count();

    public long LongCount => files.LongCount();


    public IEnumerator<TResult> GetEnumerator()
    {
        foreach(var file in files)
        {
            yield return JsonSerializer.Deserialize<TResult>(
                File.ReadAllBytes(file),
                options) ?? throw new JsonException($"Was not possible to deserialize the file {file}");
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}