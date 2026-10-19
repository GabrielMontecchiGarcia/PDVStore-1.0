/// <summary>Exportação de relatórios em PDF e Excel (equivale ao RelatorioService/ExportadorService).</summary>
public interface IExportacaoService
{
    byte[] GerarExcel<T>(IEnumerable<T> dados, string titulo) where T : class;
    byte[] GerarPdf<T>(IEnumerable<T> dados, string titulo) where T : class;
}
