using Core.Nfs;
using Core.Regimes;

namespace Core.Operacoes;

public record Saldo(
    Periodo Periodo,
    decimal Requerido,
    decimal Consumido,
    IList<Nf> NotasConsumidas
);
