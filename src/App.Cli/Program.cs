using System.CommandLine;
using Commands;

var rootCmd = new RootCommand("Calculadora de Saldos.")
{
    new CalcularSaldoCmd(),
    new BenchmarkCmd(),
    new Command("gerar", "Gerador de dados aleatórios.")
    {
        new GerarVendasCmd(),
        new GerarBomCmd()
    }
};

rootCmd.Invoke(args);
