using BenchmarkDotNet.Attributes;
using Commands;
using Core.IO;
using Core.Nfs;
using Core.Operacoes;
using Core.Produtos;
using Core.Regimes;

namespace Benchmarks;

public class BenchmarkContext
{
    public static DocumentReader<Bom>? bomReader;
    public static DocumentReader<Nf>? vendasReader;
    public static DocumentReader<Nf>? comprasReader;
    public static Periodo? periodo;

    public static CancellationToken cancellationToken;

    [Benchmark(Baseline = true)]
    public async Task RunSequencial()
    {
        var svc = new ConsumoDeSaldoSequencialService()
            .WithBoms(bomReader!)
            .WithCompras(comprasReader!)
            .WithVendas(vendasReader!)
            .WithPeriodo(periodo!);

        await svc.ExecuteAsync(cancellationToken);
    }

    public async Task RunParallel()
    {
        var svc = new ConsumoDeSaldoParaleloService()
            .WithBoms(bomReader!)
            .WithCompras(comprasReader!)
            .WithVendas(vendasReader!)
            .WithPeriodo(periodo!);

        await svc.ExecuteAsync(cancellationToken);
    }
}