using System.Text.Json.Serialization;

namespace Core.Nfs;

public record Nf(
    string Chave,
    string Numero,
    string Emitende,
    string Destinatario,
    string UfOrigem,
    string UfDestino,
    DateTime Emissao,
    IList<NfItem> Items
)
{
    [JsonIgnore]
    public decimal Saldo { get => Items.Select(x => x.ValorUnitario * x.Quantidade).Sum(); }
}