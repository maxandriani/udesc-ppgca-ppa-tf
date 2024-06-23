using Core.Nfs;
using Core.Produtos;
using Core.Regimes;

namespace Core.Operacoes;

public record Saldo(
    Periodo Periodo,
    decimal Requerido,
    decimal Consumido,
    long TotalNfsProcessadas,
    long TotalNfsUtilizadas,
    long TotalInsumos,
    Dictionary<Insumo, IList<Nf>> Consolidacao
);
