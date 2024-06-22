namespace Core.Regimes;

public record Periodo(
    short Mes,
    short Ano,
    short Apuracao
)
{
    public static Periodo Parse(string periodo)
    {
        if (periodo.Length < 9) {
            throw new ArgumentException("Período não está formatado corretamente. Deve ser composto por mês, ano e a quantidade de dias de apuração (mm/yyyy/dddd).");
        }

        short mes = short.Parse(periodo[0..2]);
        short ano = short.Parse(periodo[3..7]);
        short apuracao = short.Parse(periodo[8..]);

        if (mes == 0 || mes > 12)
            throw new ArgumentOutOfRangeException(nameof(Mes), "O mês do período precisa estar contido entre 01 e 12.");

        if (apuracao > 365*2)
            throw new ArgumentOutOfRangeException(nameof(Apuracao), "O período de apuração não pode ser superior a 2 anos.");

        return new Periodo(mes, ano, apuracao);
    }
}
