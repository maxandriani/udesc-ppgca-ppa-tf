# Fifo fiscal

Dado as características estabelecidas para o projeto, gostaria de propor a implementação simplificada de um algoritmo de consumo de saldo de estoque, também conhecido como FIFO. Proponho portanto, implementar uma versão sequencial e compara-lo com outra versão paralela com uso de threads e memória compartilhada. Para que seja possível adaptar a complexidade da proposta ao timeline, proponho o uso de uma tecnologia mais próxima da minha realidade e versão simplificadas de documentos fiscais para apenas representar a notação dos dados.

## Processo

Esse processo é a base de cálculo de consumo de estoque da maioria dos sistemas atuantes no mercado.

1. Dado um um período de apuração, geralmente 30 dias, são somadas todas as vendas de um produto ou um conjunto de produtos;
2. Para cada produto, são extraídos os saldos de consumo de insumos, ou seja, identificados e somados todas as peças necessárias para a produção (existem situações de retro alimentação que fazem desse problema um grafo circular, para efeito de adequação da proposta tratarei como uma lista finita).
3. São alimentadas todas as notas de compra de insumos de um período de tempo (30 dias até 2 anos, dependendo do regime).
4. Cada produto vendido, precisa consumir seu saldo da nota de compra mais velha, o resultado do cálculo é a lista de notas consumidas, além do total em quantidade e valor.

## Get Started

``` bash
dotnet run -- \
    --engine Sequential \
    --boms-src-dir ./ \
    --vendas-src-dir ./ \
    --compras-src-dir ./ \
    --periodo 01/2024/30 
```

``` bash
dotnet run -- \
    --engine Parallel \
    --boms-src-dir ./ \
    --vendas-src-dir ./ \
    --compras-src-dir ./ \
    --periodo 01/2024/30 
```
