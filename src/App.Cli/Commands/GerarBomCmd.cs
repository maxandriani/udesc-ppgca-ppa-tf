using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Bogus;
using Core.Produtos;
using Spectre.Console;

namespace Commands;

public class GerarBomCmd : Command
{
    static readonly Argument<long> qtdArgument = new("quantidade", "Quantidade de itens a gerar.");
    static readonly Option<DirectoryInfo> outputDirOption = new("--output-dir", "Directory to generate files.");
    static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    static readonly string fakerLocale = "pt_BR";

    public GerarBomCmd() : base("bom", "Gerar BOMs.")
    {
        AddArgument(qtdArgument);
        AddOption(outputDirOption);
        this.SetHandler(HandleAsync);
    }

    async Task HandleAsync(InvocationContext context)
    {
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Inicializando...", async spinner =>
            {
                try {
                    spinner.Status("Validando inputs...");

                    var quantity = context.ParseResult.GetValueForArgument(qtdArgument);
                    var outputDir = context.ParseResult.GetValueForOption(outputDirOption);

                    if (quantity <= 0)
                        throw new ArgumentNullException("quantidade", "Quantidade precisa ser um número positivo maior que 1.");

                    if (outputDir is null || outputDir.Exists is false)
                        throw new ArgumentNullException("--output-dir", "Você precisa informar um diretório para gerar os documentos.");

                    long bomIds = 1;
                    long produtoIds = 1;
                    long insumoIds = 1;
                    var productFaker = new Faker<Produto>(fakerLocale)
                        .CustomInstantiator(f => new Produto(++produtoIds, f.Commerce.Product()));
                    var insumoFaker = new Faker<Insumo>(fakerLocale)
                        .CustomInstantiator(f => new Insumo(new Produto(++insumoIds, f.Commerce.ProductMaterial()), f.Random.Int(1, 2000)));
                    var bomFaker = new Faker<Bom>(fakerLocale)
                        .CustomInstantiator(f => new Bom(
                            ++bomIds,
                            f.Company.CompanyName(),
                            f.Address.StateAbbr(),
                            productFaker.Generate(),
                            insumoFaker.GenerateBetween(1000, 5000)));

                    for (var idx = 1; idx <= quantity; idx++)
                    {
                        context.GetCancellationToken().ThrowIfCancellationRequested();
                        spinner.Status($"Gerando Bills of Material ({idx}/{quantity})");
                        var bomBasePath = Path.Combine(outputDir.FullName, "boms", $"{idx}.json");
                        var bomBaseDirPath = Path.GetDirectoryName(bomBasePath);
                        if (File.Exists(bomBasePath))
                            File.Delete(bomBasePath);
                        if (bomBaseDirPath is not null)
                            Directory.CreateDirectory(bomBaseDirPath);
                        
                        await using (var fileStream = File.Create(bomBasePath))
                        {
                            await JsonSerializer.SerializeAsync(fileStream, bomFaker.Generate(), jsonOptions);
                            await fileStream.FlushAsync();
                        }
                    }

                    AnsiConsole.MarkupLine($"[green]{quantity}[/] Bills of Material generated at [blue]{outputDir.FullName}/boms[/].");
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                    context.ExitCode = 1;
                }
            });
    }
}