namespace Core.Produtos;

public record Bom(
    long Codigo,
    Produto Produto,
    IList<Insumo> Insumos
);