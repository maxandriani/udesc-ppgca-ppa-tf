using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Bogus;
using Core.Nfs;
using Core.Produtos;
using Core.Regimes;
using Spectre.Console;

namespace Commands;

public class GerarVendasCmd : Command
{
    static readonly Argument<long> qtdArgument = new("quantidade", "Quantidade de itens a gerar.");
    static readonly Option<FileInfo> bomOption = new("--bom", "Bill of Material to use source base.");
    static readonly Option<DirectoryInfo> outputDirOption = new("--output-dir", "Directory to generate files.");
    static readonly Option<string> periodoOption = new (
        "--periodo",
        "Período de apuração de insumos do regime (mes/ano/apuração).");

    static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    static readonly string fakerLocale = "pt_BR";

    public GerarVendasCmd() : base("vendas", "Gerar movimentação de vendas e compras.")
    {
        AddArgument(qtdArgument);
        AddOption(bomOption);
        AddOption(periodoOption);
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
                    var bomFile = context.ParseResult.GetValueForOption(bomOption);
                    var periodo = Periodo.Parse(context.ParseResult.GetValueForOption(periodoOption) ?? throw new ArgumentNullException("--periodo", "Você precisa informar um período de apuração."));
                    var rand = new Random();

                    if (quantity <= 0)
                        throw new ArgumentNullException("quantidade", "Quantidade precisa ser um número positivo maior que 1.");

                    if (outputDir is null || outputDir.Exists is false)
                        throw new ArgumentNullException("--output-dir", "Você precisa informar um diretório para gerar os documentos.");

                    if (bomFile is null || bomFile.Exists is false)
                        throw new ArgumentNullException("--bom", "Você precida informar uma BOM válida.");

                    spinner.Status($"Loading BOM from {Markup.Escape(bomFile.FullName)}");
                    var bomStream = File.OpenRead(bomFile.FullName);
                    Bom bom = await JsonSerializer.DeserializeAsync<Bom>(bomStream, jsonOptions) ?? throw new JsonException("Was not possible to Deserialize bom.");

                    var custoInsumos = new Dictionary<long, (decimal Custo, long Requerido)>(bom.Insumos.Select(i => KeyValuePair.Create(i.Produto.Codigo, ((decimal) rand.NextDouble() % 1000M, 0L))));
                    decimal custoProduto = custoInsumos.Values.Select(i => i.Custo).Sum();

                    var vendasFaker = new Faker<Nf>(fakerLocale)
                        .CustomInstantiator(f => new Nf(
                            Guid.NewGuid().ToString(),
                            f.Random.Int(1000, 5000).ToString(),
                            bom.Company,
                            f.Company.CompanyName(),
                            bom.Uf,
                            f.Address.StateAbbr(),
                            f.Date.Between(new DateTime(periodo.Ano, periodo.Mes, 1), new DateTime(periodo.Ano, periodo.Mes, 1).AddMonths(1).AddDays(-1)),
                            new List<NfItem>() { new(1, bom.Produto.Codigo.ToString(), bom.Produto.ToString(), f.Random.Int(1, 100), custoProduto * f.Random.Decimal(1.2M, 2.5M)) }));

                    var comprasFaker = new Faker<Nf>(fakerLocale)
                        .CustomInstantiator(f => new Nf(
                            Guid.NewGuid().ToString(),
                            f.Random.Int(1000, 5000).ToString(),
                            f.Company.CompanyName(),
                            bom.Company,
                            f.Address.StateAbbr(),
                            bom.Uf,
                            f.Date.Between(new DateTime(periodo.Ano, periodo.Mes, 1).AddDays(periodo.Apuracao * -1), new DateTime(periodo.Ano, periodo.Mes, 1).AddMonths(-1)),
                            new List<NfItem>()));

                    for (var idx = 1; idx <= quantity; idx++)
                    {
                        context.GetCancellationToken().ThrowIfCancellationRequested();
                        spinner.Status($"Nf de venda ({idx}/{quantity})");
                        var nfVenda = vendasFaker.Generate();
                        
                        await WriteNfAsync(outputDir, "vendas", nfVenda);

                        foreach (var insumo in bom.Insumos)
                        {
                            var item = custoInsumos[insumo.Produto.Codigo];
                            item.Requerido += (long)insumo.Quantidade * (long)nfVenda.Items.Sum(x => x.Quantidade);
                            custoInsumos[insumo.Produto.Codigo] = item;
                        }
                    }

                    long totalNfCompra = 0;
                    foreach (var insumo in bom.Insumos)
                    {
                        context.GetCancellationToken().ThrowIfCancellationRequested();
                        var (custo, saldo) = custoInsumos[insumo.Produto.Codigo];
                        spinner.Status($"Gerando notas para o insumo {Markup.Escape(insumo.Produto.Descricao)} com saldo {saldo.ToString("D2")}");
                        while (saldo > 0)
                        {
                            totalNfCompra++;
                            spinner.Status($"Nf de compra ({totalNfCompra})");
                            var saldoParcial = rand.NextInt64(1, saldo);
                            var nfCompra = comprasFaker.Generate();
                            nfCompra.Items.Add(new NfItem(1, insumo.Produto.Codigo.ToString(), insumo.Produto.ToString(), saldoParcial, custo));

                            await WriteNfAsync(outputDir, "compras", nfCompra);

                            saldo -= saldoParcial;
                        }
                    }

                    AnsiConsole.MarkupLine($"[green]{quantity}[/] Nfs de vendas geradas em [blue]{Markup.Escape(outputDir.FullName + "/vendas")}[/].");
                    AnsiConsole.MarkupLine($"[green]{totalNfCompra}[/] Nfs de compras geradas em [blue]{Markup.Escape(outputDir.FullName + "/compras")}[/].");
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    context.ExitCode = 1;
                }
            });
    }

    private async Task WriteNfAsync(DirectoryInfo outputDir, string path, Nf nf)
    {
        var nfPath = Path.Combine(outputDir.FullName, path, nf.Emissao.Year.ToString(), nf.Emissao.Month.ToString("D2"), $"{nf.Chave}.json");
        var nfDirPath = Path.GetDirectoryName(nfPath);
        if (File.Exists(nfPath))
            File.Delete(nfPath);
        if (nfDirPath is not null)
            Directory.CreateDirectory(nfDirPath);
        
        await using (var fileStream = File.Create(nfPath))
        {
            await JsonSerializer.SerializeAsync(fileStream, nf, jsonOptions);
            await fileStream.FlushAsync();
        }
    }
}