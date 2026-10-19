using PDVStore.Application.Interfaces;
using PDVStore.Domain.Common;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de clientes. Preserva as regras do ClienteService:
/// - Nome obrigatório;
/// - Cadastro novo grava CadastradoEm em UTC e inicia ativo;
/// - Edição não mexe no saldo devedor (somente venda fiada/recebimento/cancelamento);
/// - Status é alterado logicamente (soft delete);
/// - Recebimento de fiado exige valor &gt; 0 e nunca deixa saldo negativo.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(IClienteRepository clientes, IUnitOfWork unitOfWork)
    {
        _clientes = clientes;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Cliente>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default) =>
        _clientes.ListarAsync(apenasAtivos, filtro?.Trim(), ct);

    public Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _clientes.ObterPorIdAsync(id, rastrear: false, ct);

    /// <summary>Cadastra um cliente ativo, normalizando CPF/CNPJ e telefone com máscara.</summary>
    public async Task<Cliente> CriarAsync(ClienteRequest request, CancellationToken ct = default)
    {
        var cliente = new Cliente
        {
            Ativo = true,
            CadastradoEm = DateTime.UtcNow
        };

        Aplicar(request, cliente);

        await _clientes.AdicionarAsync(cliente, ct);
        await _unitOfWork.SalvarAsync(ct);
        return cliente;
    }

    /// <summary>
    /// Atualiza os dados cadastrais de um cliente existente. O saldo devedor é
    /// preservado; somente o limite de crédito pode ser alterado neste fluxo.
    /// </summary>
    public async Task<Cliente> AtualizarAsync(int id, ClienteRequest request, CancellationToken ct = default)
    {
        var cliente = await _clientes.ObterPorIdAsync(id, rastrear: true, ct)
            ?? throw new NaoEncontradoException("Cliente não encontrado.");

        Aplicar(request, cliente);
        await _unitOfWork.SalvarAsync(ct);
        return cliente;
    }

    /// <summary>Ativa/desativa o cliente (usado como exclusão lógica e reativação).</summary>
    public async Task<bool> AtualizarStatusAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var cliente = await _clientes.ObterPorIdAsync(id, rastrear: true, ct);
        if (cliente == null) return false;

        cliente.Ativo = ativo;
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    /// <summary>
    /// Baixa parcial ou total do saldo devedor. O valor deve ser maior que zero;
    /// o saldo é reduzido com Math.Max(0, ...) para nunca ficar negativo.
    /// </summary>
    public async Task<bool> ReceberFiadoAsync(int clienteId, decimal valor, CancellationToken ct = default)
    {
        if (valor <= 0)
            throw new RegraDeNegocioException("Valor deve ser maior que zero.");

        var cliente = await _clientes.ObterPorIdAsync(clienteId, rastrear: true, ct);
        if (cliente == null) return false;

        cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - valor);
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    private static void Aplicar(ClienteRequest request, Cliente cliente)
    {
        cliente.Nome = request.Nome.Trim();
        cliente.CpfCnpj = Mascaras.NormalizarDocumento(request.CpfCnpj);
        cliente.Telefone = Mascaras.NormalizarTelefone(request.Telefone);
        cliente.Email = request.Email?.Trim();
        cliente.Endereco = request.Endereco?.Trim();
        cliente.LimiteCredito = Math.Max(0, request.LimiteCredito);
    }
}
