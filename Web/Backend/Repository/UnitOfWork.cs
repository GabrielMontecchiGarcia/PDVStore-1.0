using Microsoft.EntityFrameworkCore.Storage;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Repository;

/// <summary>
/// Implementação do Unit of Work sobre o PDVStoreDbContext.
/// Cada requisição HTTP recebe um DbContext scoped (registrado na API), portanto
/// todos os repositórios e serviços da mesma requisição participam da mesma transação.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly PDVStoreDbContext _context;

    public UnitOfWork(PDVStoreDbContext context) => _context = context;

    // Persiste todas as alterações rastreadas pelo EF Core.
    public Task<int> SalvarAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    // Inicia uma transação explícita (usada por vendas, compras e cancelamentos).
    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        var transacao = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new TransacaoEf(transacao);
    }

    /// <summary>Adapter que expõe a transação do EF como ITransacao da aplicação.</summary>
    private sealed class TransacaoEf : ITransacao
    {
        private readonly IDbContextTransaction _transacao;

        public TransacaoEf(IDbContextTransaction transacao) => _transacao = transacao;

        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            _transacao.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            _transacao.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => _transacao.DisposeAsync();
    }
}
