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
dotnet run --project ./src/App.Cli -- gerar bom 10 --output-dir ./inputs
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/1.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/2.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/3.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/4.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/5.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/6.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/7.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/8.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/9.json
dotnet run --project ./src/App.Cli -- gerar vendas 10000 --output-dir ./inputs --periodo 06/2024/365 --bom ./inputs/boms/10.json

```

``` bash
dotnet run --project ./src/App.Cli -- calcular --boms-src-dir ./inputs/boms --compras-src-dir ./inputs/compras --vendas-src-dir ./inputs/vendas --periodo 06/2024/365 --engine Sequential
```

``` bash
dotnet run --project ./src/App.Cli -- calcular --boms-src-dir ./inputs/boms --compras-src-dir ./inputs/compras --vendas-src-dir ./inputs/vendas --periodo 06/2024/365 --engine Parallel
```

``` bash
sudo dotnet run -c release --project ./src/App.Benchmark -- --envVars INPUTS:/Users/maxandriani/Projects/udesc-ppgca-ppa-tf/inputs
```
