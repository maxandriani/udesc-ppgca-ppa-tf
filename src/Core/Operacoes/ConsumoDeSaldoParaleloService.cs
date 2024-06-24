using System.Collections.Concurrent;
using Core.Nfs;
using Core.Produtos;
using Core.Regimes;

namespace Core.Operacoes;

public class ConsumoDeSaldoParaleloService : ConsumoDeSaldoService
{
    private Periodo? periodo = null;
    private IEnumerable<Bom>? bomsReader = null;
    private IEnumerable<Nf>? vendasReader = null;
    private IEnumerable<Nf>? comprasReader = null;

    public Task<Saldo> ExecuteAsync(CancellationToken cancellationToken)
    {
        // Validar se posso rodas
        if (periodo is null) throw new ArgumentNullException(nameof(periodo), "Período é um campo requerido para o processamento.");
        if (bomsReader is null || bomsReader.Any() is false) throw new ArgumentNullException(nameof(bomsReader), "Boms é um campo requerido para o processamento.");
        if (vendasReader is null || vendasReader.Any() is false) throw new ArgumentNullException(nameof(vendasReader), "NFs de vendas são requeridas para o processamento.");
        if (comprasReader is null || comprasReader.Any() is false) throw new ArgumentNullException(nameof(comprasReader), "NFs de compras são requeridas para o processamento.");

        // 1. Carregar BOMs
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs
        // https://github.com/microsoft/referencesource/blob/master/System.Core/System/Linq/Enumerable.cs
        cancellationToken.ThrowIfCancellationRequested();
        var boms = bomsReader
            .AsParallel()
            .ToDictionary(bom => bom.Produto.Codigo);                                    

        // 2. Carregar Notas de Vendas e extrair os itens consumidos com seus saldos.
        cancellationToken.ThrowIfCancellationRequested();
        var produtosVendidos = vendasReader
            .AsParallel()
            .SelectMany(nf => nf.Items) // log(xˆ2)
            .Select(nfItem => new { CodigoProduto = nfItem.SerialNumber, nfItem.Quantidade })
            .GroupBy(p => p.CodigoProduto); // Esse GroupBy materializa a porra toda na memória...

        // 3. Consolidar listas de insumos e saldos requeridos
        // Eu não tenho como adivinhar quantos insumos vou precisar, mas posso apostar no máximo com base nas BOMs que conheco.
        var insumosRequeridos = new ConcurrentDictionary<long, decimal>(Environment.ProcessorCount, boms.Values.Select(b => b.Insumos.Count()).Sum());

        Parallel.ForEach(produtosVendidos, (produtos) => // Equivalente ao openmp for
        {
            var key = long.Parse(produtos.Key);
            var quantidadeVendida = produtos.Sum(p => p.Quantidade);
            if (!boms.ContainsKey(key)) throw new Exception($"Não foi encontrada BOM para o produto {produtos.Key}.");
            var bom = boms[key];

            foreach(var insumo in bom.Insumos)
            {
                if (!insumosRequeridos.ContainsKey(insumo.Produto.Codigo))
                {
                    insumosRequeridos[insumo.Produto.Codigo] = quantidadeVendida * insumo.Quantidade;
                }
                else
                {
                    insumosRequeridos[insumo.Produto.Codigo] += quantidadeVendida * insumo.Quantidade;
                }
            }
        });

        // 4. Carregar NFs e desprezar NFs não relevantes
        // 5. Ordenar NFs
        cancellationToken.ThrowIfCancellationRequested();
        var totalCompras = comprasReader.LongCount();
        var nfsCompras = comprasReader
            .AsParallel()
            .Where(p => p.Items.Any(i => insumosRequeridos.ContainsKey(long.Parse(i.SerialNumber)))) // Isso é rápido
            .OrderBy(p => p.Emissao)
            .SelectMany(nf => nf.Items.Select(item => new { Nf = nf, Item = item }))
            .ToList();

        // 6. Calcular consumo até atingir saldo...
        // 7. Retornar NFs utilizadas por Insumo
        cancellationToken.ThrowIfCancellationRequested();
        decimal saldoRequerido = insumosRequeridos.Values.Sum();
        //decimal saldoConsumido = 0;
        // long TotalNfsUtilizadas = 0;
        var saldoConsumidoBag = new ConcurrentBag<decimal>();
        var totalNfUtilizadasBag = new ConcurrentBag<long>();
        var nfsConsumidasPorInsumo = new ConcurrentDictionary<long, IList<Nf>>(Environment.ProcessorCount, insumosRequeridos.Count);

        Parallel.ForEach(insumosRequeridos.Chunk(Environment.ProcessorCount), insumos =>
        {
            long totalNfsUtilizadas = 0;
            decimal saldoConsumido = 0;
            var insumosParticionados = new Dictionary<long, decimal>(insumos);
            foreach(var nfCompra in nfsCompras.Where(nf => insumosParticionados.ContainsKey(long.Parse(nf.Item.SerialNumber))))
            {
                var key = long.Parse(nfCompra.Item.SerialNumber);
                if (!insumosParticionados.ContainsKey(key) || insumosParticionados[key] < 0)
                {
                    return;
                }

                insumosParticionados[key] -= nfCompra.Item.Quantidade;
                saldoConsumido += nfCompra.Item.Quantidade;

                if (!nfsConsumidasPorInsumo.ContainsKey(key))
                {
                    nfsConsumidasPorInsumo[key] = new List<Nf>();
                }

                nfsConsumidasPorInsumo[key].Add(nfCompra.Nf);
                totalNfsUtilizadas++;
            }
            totalNfUtilizadasBag.Add(totalNfsUtilizadas);
            saldoConsumidoBag.Add(saldoConsumido);
        });

        cancellationToken.ThrowIfCancellationRequested();
        var insumos = boms.Values
            .AsParallel()
            .SelectMany(bom => bom.Insumos)
            .Where(insumo => nfsConsumidasPorInsumo.ContainsKey(insumo.Produto.Codigo))
            .ToDictionary(p => p.Produto.Codigo);

        return Task.FromResult(new Saldo(
            periodo!,
            saldoRequerido,
            saldoConsumidoBag.Sum(),
            totalCompras,
            totalNfUtilizadasBag.Sum(),
            insumosRequeridos.Count,
            new Dictionary<Insumo, IList<Nf>>(nfsConsumidasPorInsumo.Select(kvp => KeyValuePair.Create(insumos[kvp.Key], kvp.Value)))));
    }

    public ConsumoDeSaldoService WithBoms(IEnumerable<Bom> boms)
    {
        bomsReader = boms;
        return this;
    }

    public ConsumoDeSaldoService WithCompras(IEnumerable<Nf> nfs)
    {
        comprasReader = nfs;
        return this;
    }

    public ConsumoDeSaldoService WithVendas(IEnumerable<Nf> nfs)
    {
        vendasReader = nfs;
        return this;
    }

    public ConsumoDeSaldoService WithPeriodo(Periodo periodo)
    {
        this.periodo = periodo;
        return this;
    }
}