using BenchmarkDotNet.Attributes;
using Core.IO;
using Core.Nfs;
using Core.Operacoes;
using Core.Produtos;
using Core.Regimes;

public class BenchmarkContext
{
    public DocumentReader<Bom>? bomReader;
    public DocumentReader<Nf>? vendasReader;
    public DocumentReader<Nf>? comprasReader;
    public Periodo periodo = new Periodo(6, 2024, 365);

    [GlobalSetup]
    public void Setup()
    {
        var basePath = Path.GetFullPath(Environment.GetEnvironmentVariable("INPUT_DIR") ?? Path.Combine(Environment.CurrentDirectory, "inputs"));
        bomReader = new(Path.Combine(basePath, "boms"));
        vendasReader = new(Path.Combine(basePath, "vendas"));
        comprasReader = new(Path.Combine(basePath, "compras"));
    }

    [Benchmark(Baseline = true)]
    public async Task RunSequencial()
    {
        var svc = new ConsumoDeSaldoSequencialService()
            .WithBoms(bomReader!)
            .WithCompras(comprasReader!)
            .WithVendas(vendasReader!)
            .WithPeriodo(periodo!);

        await svc.ExecuteAsync(CancellationToken.None);
    }

    [Benchmark]
    public async Task RunParallel()
    {
        var svc = new ConsumoDeSaldoParaleloService()
            .WithBoms(bomReader!)
            .WithCompras(comprasReader!)
            .WithVendas(vendasReader!)
            .WithPeriodo(periodo!);

        await svc.ExecuteAsync(CancellationToken.None);
    }
}