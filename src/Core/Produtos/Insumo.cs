namespace Core.Produtos;

public record Insumo(
    Produto produto,
    decimal quantidade
) {
    public override string ToString() {
        return produto.ToString();
    }
}