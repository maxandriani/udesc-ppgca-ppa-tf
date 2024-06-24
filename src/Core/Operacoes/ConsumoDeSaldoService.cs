using Core.IO;
using Core.Nfs;
using Core.Produtos;
using Core.Regimes;

namespace Core.Operacoes;

public interface ConsumoDeSaldoService
{
    ConsumoDeSaldoService WithBoms(DocumentReader<Bom> boms);
    ConsumoDeSaldoService WithVendas(DocumentReader<Nf> nfs);
    ConsumoDeSaldoService WithCompras(DocumentReader<Nf> nfs);
    ConsumoDeSaldoService WithPeriodo(Periodo periodo);
    Task<Saldo> ExecuteAsync(CancellationToken cancellationToken, IList<(string Step, TimeSpan Time)>? timeCounter = null);
}
