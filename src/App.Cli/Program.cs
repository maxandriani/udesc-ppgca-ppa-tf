using System.CommandLine;
using Core.IO;
using Core.Nfs;
using Core.Operacoes;
using Core.Produtos;
using Core.Regimes;

var engineOpt = new Option<EngineType>(
    "--engine",
    () => EngineType.Sequential,
    "Select the engine: Sequential or Parallel.");

var vendasSrcDirOpt = new Option<DirectoryInfo>(
    "--vendas-src-dir",
    "Diretório contendo as Nfs de Vendas do Período.");

var comprasSrcDirOpt = new Option<DirectoryInfo>(
    "--compras-src-dir",
    "Diretório contendo as Nfs de Compras de insumos do Período.");

var bomsSrcDirOpt = new Option<DirectoryInfo>(
    "--boms-src-dir",
    "Diretório contendo as boms.");

var periodoOpt = new Option<string>(
    "--periodo",
    "Período de apuração de insumos do regime (mes/ano/apuração).");

var rootCmd = new RootCommand("Cálculo de Saldo do Regime")
{
    engineOpt,
    bomsSrcDirOpt,
    vendasSrcDirOpt,
    comprasSrcDirOpt,
    periodoOpt
};

rootCmd.SetHandler(async (context) =>
{
    var engine = context.ParseResult.GetValueForOption(engineOpt);
    var bomsSrcDir = context.ParseResult.GetValueForOption(bomsSrcDirOpt);
    var vendasSrcDir = context.ParseResult.GetValueForOption(vendasSrcDirOpt);
    var comprasSrcDir = context.ParseResult.GetValueForOption(comprasSrcDirOpt);
    var periodo = context.ParseResult.GetValueForOption(periodoOpt);

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

        var result = await svc
            .WithBoms(bomReader)
            .WithVendas(vendasReader)
            .WithCompras(comprasReader)
            .WithPeriodo(Periodo.Parse(periodo))
            .ExecuteAsync(context.GetCancellationToken());

        // Print Result
        Console.WriteLine($"Período: {result.Periodo.Mes}/{result.Periodo.Ano} {result.Periodo.Apuracao} dias.");
        Console.WriteLine($"Saldo Requerido: {result.Requerido}.");
        Console.WriteLine($"Saldo Consumido: {result.Consumido}.");
        Console.WriteLine($"Total NFs consumidas: {result.NotasConsumidas.Count()}.");
    }
    catch (Exception ex)
    {
        context.ExitCode = 1;
        Console.WriteLine(ex.Message);
    }
});

rootCmd.Invoke(args);
