using System.CommandLine;
using System.CommandLine.Invocation;
using BenchmarkDotNet.Running;
using Benchmarks;
using Core.IO;
using Core.Nfs;
using Core.Produtos;
using Core.Regimes;
using Spectre.Console;

namespace Commands;

public class BenchmarkCmd : Command
{
    static readonly Option<DirectoryInfo> vendasOption = new (
        "--vendas-src-dir",
        "Diretório contendo as Nfs de Vendas do Período.");

    static readonly Option<DirectoryInfo> comprasOption = new (
        "--compras-src-dir",
        "Diretório contendo as Nfs de Compras de insumos do Período.");

    static readonly Option<DirectoryInfo> bomsDirOptions = new (
        "--boms-src-dir",
        "Diretório contendo as boms.");

    static readonly Option<string> periodoOption = new (
        "--periodo",
        "Período de apuração de insumos do regime (mes/ano/apuração).");

    public BenchmarkCmd() : base("benchmark", "Run Benchmark.")
    {
        AddOption(vendasOption);
        AddOption(comprasOption);
        AddOption(bomsDirOptions);
        AddOption(periodoOption);

        this.SetHandler(HandleAsync);
    }

    Task HandleAsync(InvocationContext context)
    {
        var bomsSrcDir = context.ParseResult.GetValueForOption(bomsDirOptions);
        var vendasSrcDir = context.ParseResult.GetValueForOption(vendasOption);
        var comprasSrcDir = context.ParseResult.GetValueForOption(comprasOption);
        var periodo = context.ParseResult.GetValueForOption(periodoOption);

        try {
            if (bomsSrcDir is null || bomsSrcDir.Exists is false)
                throw new DirectoryNotFoundException("You should provide a valid Bom Source Directory.");

            if (vendasSrcDir is null || vendasSrcDir.Exists is false)
                throw new DirectoryNotFoundException("You should provide a valid Vendas Source Directory.");

            if (comprasSrcDir is null || comprasSrcDir.Exists is false)
                throw new DirectoryNotFoundException("You should provide a valid Compras Source Directory.");

            if (periodo is null || string.IsNullOrWhiteSpace(periodo))
                throw new ArgumentNullException("You should provide a valid Periodo option.");

            BenchmarkContext.bomReader = new DocumentReader<Bom>(bomsSrcDir.FullName);
            BenchmarkContext.comprasReader = new DocumentReader<Nf>(comprasSrcDir.FullName);
            BenchmarkContext.vendasReader = new DocumentReader<Nf>(vendasSrcDir.FullName);
            BenchmarkContext.periodo = Periodo.Parse(periodo);
            BenchmarkContext.cancellationToken = context.GetCancellationToken();

            BenchmarkRunner.Run<BenchmarkContext>();
        } catch (Exception ex) {
            context.ExitCode = 1;
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
        return Task.CompletedTask;
    }
}