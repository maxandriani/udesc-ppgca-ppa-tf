namespace Core.Produtos;

public record Insumo(
    Produto Produto,
    decimal Quantidade
) {
    public override string ToString() {
        return Produto.ToString();
    }
}