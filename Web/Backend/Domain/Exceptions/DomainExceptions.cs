namespace PDVStore.Domain.Exceptions;

/// <summary>
/// Exceção base para erros de regra de negócio conhecidos. O middleware global
/// converte para HTTP 400 com a mensagem original em português.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Violação de regra de negócio (estoque insuficiente, caixa fechado, limite
/// de crédito etc.). Mapeada para HTTP 400.
/// </summary>
public class RegraDeNegocioException : DomainException
{
    public RegraDeNegocioException(string message) : base(message) { }
}

/// <summary>
/// Recurso inexistente. Mapeada para HTTP 404.
/// </summary>
public class NaoEncontradoException : DomainException
{
    public NaoEncontradoException(string message) : base(message) { }
}

/// <summary>
/// Acesso não autorizado ao recurso/perfil. Mapeada para HTTP 403.
/// </summary>
public class AcessoNegadoException : DomainException
{
    public AcessoNegadoException(string message) : base(message) { }
}
