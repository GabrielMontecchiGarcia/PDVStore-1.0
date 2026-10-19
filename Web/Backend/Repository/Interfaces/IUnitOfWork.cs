namespace PDVStore.Repository.Interfaces;

/// <summary>
/// Transação de banco de dados controlada pela camada de serviço. Garante o
/// mesmo comportamento do projeto original: tudo ou nada em vendas/compras/caixa.
/// </summary>
public interface ITransacao : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work: centraliza o SaveChanges e o controle transacional para que
/// todos os repositórios compartilhem o mesmo DbContext scoped por requisição.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SalvarAsync(CancellationToken cancellationToken = default);
    Task<ITransacao> IniciarTransacaoAsync(CancellationToken cancellationToken = default);
}
