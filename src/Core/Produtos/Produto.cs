namespace Core.Produtos;

public record Produto(
    long Codigo,
    string Descricao
)
{
    public override string ToString() {
        return $"{Codigo}: {Descricao}";
    }
}