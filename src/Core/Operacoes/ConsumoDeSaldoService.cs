using Core.Nfs;
using Core.Produtos;
using Core.Regimes;

namespace Core.Operacoes;

public interface ConsumoDeSaldoService
{
    ConsumoDeSaldoService WithBoms(IEnumerable<Bom> boms);
    ConsumoDeSaldoService WithVendas(IEnumerable<Nf> nfs);
    ConsumoDeSaldoService WithCompras(IEnumerable<Nf> nfs);
    ConsumoDeSaldoService WithPeriodo(Periodo periodo);
    Task<Saldo> ExecuteAsync(CancellationToken cancellationToken);
}
