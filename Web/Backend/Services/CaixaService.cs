using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de caixa. Preserva integralmente as regras do CaixaService:
/// - Um único caixa aberto por vez;
/// - Abertura registra valor inicial e usuário responsável em UTC;
/// - Fechamento = ValorInicial + vendas concluídas do caixa - sangrias;
/// - Sangria exige valor &gt; 0 e só é permitida com caixa aberto.
/// </summary>
public class CaixaService : ICaixaService
{
    private readonly ICaixaRepository _caixas;
    private readonly IVendaRepository _vendas;
    private readonly IUnitOfWork _unitOfWork;

    public CaixaService(ICaixaRepository caixas, IVendaRepository vendas, IUnitOfWork unitOfWork)
    {
        _caixas = caixas;
        _vendas = vendas;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Abre um novo caixa, bloqueando quando já existe outro aberto.</summary>
    public async Task<Caixa> AbrirCaixaAsync(decimal valorInicial, int usuarioCaixaId, CancellationToken ct = default)
    {
        var existente = await _caixas.ObterAbertoAsync(rastrear: false, ct);
        if (existente != null)
            throw new RegraDeNegocioException("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.");

        var caixa = new Caixa
        {
            UsuarioCaixaId = usuarioCaixaId,
            Abertura = DateTime.UtcNow,
            ValorInicial = valorInicial,
            Status = StatusCaixa.Aberto
        };

        await _caixas.AdicionarAsync(caixa, ct);
        await _unitOfWork.SalvarAsync(ct);
        return caixa;
    }

    public Task<Caixa?> ObterCaixaAbertoAsync(CancellationToken ct = default) =>
        _caixas.ObterAbertoAsync(rastrear: false, ct);

    /// <summary>
    /// Fecha o caixa calculando o valor final. Retorna false (sem exceção) quando
    /// o caixa não existe ou já está fechado, como no serviço original.
    /// </summary>
    public async Task<bool> FecharCaixaAsync(int caixaId, CancellationToken ct = default)
    {
        var caixa = await _caixas.ObterPorIdAsync(caixaId, rastrear: true, ct);

        if (caixa == null || caixa.Status != StatusCaixa.Aberto)
            return false;

        decimal totalVendas = await _vendas.SomarVendasDoCaixaAsync(caixaId, ct);
        decimal sangria = caixa.Sangria ?? 0;

        caixa.ValorFinal = caixa.ValorInicial + totalVendas - sangria;
        caixa.Fechamento = DateTime.UtcNow;
        caixa.Status = StatusCaixa.Fechado;

        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    /// <summary>
    /// Registra sangria acumulando o valor no caixa. O valor deve ser positivo e
    /// o caixa precisa estar aberto; caso contrário retorna false.
    /// </summary>
    public async Task<bool> RegistrarSangriaAsync(int caixaId, decimal valor, CancellationToken ct = default)
    {
        if (valor <= 0)
            throw new RegraDeNegocioException("Valor da sangria deve ser maior que zero.");

        var caixa = await _caixas.ObterPorIdAsync(caixaId, rastrear: true, ct);
        if (caixa == null || caixa.Status != StatusCaixa.Aberto)
            return false;

        caixa.Sangria = (caixa.Sangria ?? 0) + valor;
        caixa.ValorFinal ??= caixa.ValorInicial;

        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    public Task<List<Caixa>> ListarAsync(CancellationToken ct = default) =>
        _caixas.ListarAsync(ct);
}
