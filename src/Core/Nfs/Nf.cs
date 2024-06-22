using System.Collections.Generic;

namespace Core.Nfs;

public record Nf(
    string Chave,
    string Numero,
    short Ano,
    string Emitende,
    string Destinatario,
    string UfOrigem,
    string UfDestino,
    IList<NfItem> Items
)
{
    public decimal Saldo { get => Items.Select(x => x.ValorUnitario * x.Quantidade).Sum(); }
}