namespace PDVStore.API.Common;

/// <summary>
/// Converte datas (yyyy-MM-dd) recebidas do frontend em intervalos UTC.
/// O banco armazena UTC, mas o operador raciocina em data local; o fuso vem de
/// App:TimeZone. Quando não informado, o período padrão é do 1º dia do mês até hoje,
/// exatamente como o Dashboard original (dtpInicio/dtpFim).
/// </summary>
public class PeriodoService
{
    private readonly TimeZoneInfo _timeZone;

    public PeriodoService(TimeZoneInfo timeZone) => _timeZone = timeZone;

    public (DateTime InicioUtc, DateTime FimUtc) Converter(DateTime? inicio, DateTime? fim)
    {
        var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone).Date;

        var inicioLocal = (inicio?.Date) ?? new DateTime(hojeLocal.Year, hojeLocal.Month, 1);
        var fimLocal = (fim?.Date) ?? hojeLocal;

        // Converte a borda inicial (00:00:00) e final (23:59:59.9999999) para UTC.
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(inicioLocal, DateTimeKind.Unspecified), _timeZone);

        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(fimLocal.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified), _timeZone);

        return (inicioUtc, fimUtc);
    }
}
