namespace Core.Nfs;

public record NfItem(
    long Numero,
    string SerialNumber,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario
);