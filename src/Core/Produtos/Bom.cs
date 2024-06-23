namespace Core.Produtos;

public record Bom(
    long Codigo,
    string Company,
    string Uf,
    Produto Produto,
    IList<Insumo> Insumos
);