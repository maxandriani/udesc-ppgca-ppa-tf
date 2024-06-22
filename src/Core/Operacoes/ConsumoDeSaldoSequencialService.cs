using Core.Nfs;
using Core.Produtos;
using Core.Regimes;

namespace Core.Operacoes;

public class ConsumoDeSaldoSequencialService : ConsumoDeSaldoService
{
    private Periodo? periodo = null;
    private IEnumerable<Bom> boms = new List<Bom>();
    private IEnumerable<Nf> vendas = new List<Nf>();
    private IEnumerable<Nf> compras = new List<Nf>();

    public Task<Saldo> ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Engine: Sequencial.");
        return Task.FromResult(new Saldo(periodo!, 0, 0, new List<Nf>()));
    }

    public ConsumoDeSaldoService WithBoms(IEnumerable<Bom> boms)
    {
        this.boms = boms;
        return this;
    }

    public ConsumoDeSaldoService WithCompras(IEnumerable<Nf> nfs)
    {
        compras = nfs;
        return this;
    }

    public ConsumoDeSaldoService WithVendas(IEnumerable<Nf> nfs)
    {
        vendas = nfs;
        return this;
    }

    public ConsumoDeSaldoService WithPeriodo(Periodo periodo)
    {
        this.periodo = periodo;
        return this;
    }
}