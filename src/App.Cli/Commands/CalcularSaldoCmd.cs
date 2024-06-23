using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using Core.IO;
using Core.Nfs;
using Core.Operacoes;
using Core.Produtos;
using Core.Regimes;
using Spectre.Console;

namespace Commands;

public class CalcularSaldoCmd : Command
{
    static readonly Option<EngineType> engineOption = new (
        "--engine",
        () => EngineType.Sequential,
        "Select the engine: Sequential or Parallel.");

    static readonly Option<DirectoryInfo> vendasSrcDirOption = new (
        "--vendas-src-dir",
        "Diretório contendo as Nfs de Vendas do Período.");

    static readonly Option<DirectoryInfo> comprasSrcDirOption = new (
        "--compras-src-dir",
        "Diretório contendo as Nfs de Compras de insumos do Período.");

    static readonly Option<DirectoryInfo> bomsSrcDirOption = new (
        "--boms-src-dir",
        "Diretório contendo as boms.");

    static readonly Option<string> periodoOption = new (
        "--periodo",
        "Período de apuração de insumos do regime (mes/ano/apuração).");

    public CalcularSaldoCmd() : base("calcular", "Cálculo de Saldo do Regime")
    {
        AddOption(engineOption);
        AddOption(vendasSrcDirOption);
        AddOption(comprasSrcDirOption);
        AddOption(bomsSrcDirOption);
        AddOption(periodoOption);

        this.SetHandler(HandleAsync);
    }

    async Task HandleAsync(InvocationContext context)
    {
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Validando dados", async spinner =>
        {
            var engine = context.ParseResult.GetValueForOption(engineOption);
            var bomsSrcDir = context.ParseResult.GetValueForOption(bomsSrcDirOption);
            var vendasSrcDir = context.ParseResult.GetValueForOption(vendasSrcDirOption);
            var comprasSrcDir = context.ParseResult.GetValueForOption(comprasSrcDirOption);
            var periodo = context.ParseResult.GetValueForOption(periodoOption);

            try {

                // Guards
                if (bomsSrcDir is null || bomsSrcDir.Exists is false)
                    throw new DirectoryNotFoundException("You should provide a valid Bom Source Directory.");

                if (vendasSrcDir is null || vendasSrcDir.Exists is false)
                    throw new DirectoryNotFoundException("You should provide a valid Vendas Source Directory.");

                if (comprasSrcDir is null || comprasSrcDir.Exists is false)
                    throw new DirectoryNotFoundException("You should provide a valid Compras Source Directory.");

                if (periodo is null || string.IsNullOrWhiteSpace(periodo))
                    throw new ArgumentNullException("You should provide a valid Periodo option.");

                ConsumoDeSaldoService svc;

                switch (engine)
                {
                    case EngineType.Sequential:
                        svc = new ConsumoDeSaldoSequencialService();
                        break;
                    case EngineType.Parallel:
                        svc = new ConsumoDeSaldoParaleloService();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("You must set an valid engine, 1 for sequential ou 2 for parallel.");
                }

                var bomReader = new DocumentReader<Bom>(bomsSrcDir.FullName);
                var vendasReader = new DocumentReader<Nf>(vendasSrcDir.FullName);
                var comprasReader = new DocumentReader<Nf>(comprasSrcDir.FullName);

                spinner.Status("Calculando saldo...");
                var result = await svc
                    .WithBoms(bomReader)
                    .WithVendas(vendasReader)
                    .WithCompras(comprasReader)
                    .WithPeriodo(Periodo.Parse(periodo))
                    .ExecuteAsync(context.GetCancellationToken());

                // Print Result
                var table = new Table();
                table.AddColumn("Período");
                table.AddColumn("Saldo Requerido");
                table.AddColumn("Saldo Consumido");
                table.AddColumn("Total NFs Processadas");
                table.AddColumn("Total NFs Utilizadas");
                table.AddColumn("Total Insumos");

                table.AddRow(
                    $"{result.Periodo.Mes.ToString("D2")}/{result.Periodo.Ano} {result.Periodo.Apuracao} dias.",
                    result.Requerido.ToString("C2", CultureInfo.CreateSpecificCulture("pt-BR")),
                    result.Consumido.ToString("C2", CultureInfo.CreateSpecificCulture("pt-BR")),
                    result.TotalNfsProcessadas.ToString("N", CultureInfo.CreateSpecificCulture("pt-BR")),
                    result.TotalNfsUtilizadas.ToString("N", CultureInfo.CreateSpecificCulture("pt-BR")),
                    result.TotalInsumos.ToString("N", CultureInfo.CreateSpecificCulture("pr-BR")));

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                context.ExitCode = 1;
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            }
        });
    }
}