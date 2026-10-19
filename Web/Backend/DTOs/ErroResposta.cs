namespace PDVStore.DTOs;

/// <summary>
/// Formato padrão de erro da API (Problem Details simplificado).
/// Erros de validação de entrada trazem a lista em `detalhes`, sempre em PT-BR,
/// preservando as mensagens exibidas pelo sistema Windows.
/// </summary>
public class ErroResposta
{
    public int Status { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public List<string>? Detalhes { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
