using PDVStore.Application.Interfaces;
using PDVStore.Domain.Common;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de fornecedores. Preserva o upsert com nome obrigatório e a
/// desativação lógica que protege o histórico de compras.
/// </summary>
public class FornecedorService : IFornecedorService
{
    private readonly IFornecedorRepository _fornecedores;
    private readonly IUnitOfWork _unitOfWork;

    public FornecedorService(IFornecedorRepository fornecedores, IUnitOfWork unitOfWork)
    {
        _fornecedores = fornecedores;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Fornecedor>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default) =>
        _fornecedores.ListarAsync(apenasAtivos, filtro?.Trim(), ct);

    public Task<Fornecedor?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _fornecedores.ObterPorIdAsync(id, rastrear: false, ct);

    public async Task<Fornecedor> CriarAsync(FornecedorRequest request, CancellationToken ct = default)
    {
        var fornecedor = new Fornecedor { Ativo = true };
        Aplicar(request, fornecedor);

        await _fornecedores.AdicionarAsync(fornecedor, ct);
        await _unitOfWork.SalvarAsync(ct);
        return fornecedor;
    }

    public async Task<Fornecedor> AtualizarAsync(int id, FornecedorRequest request, CancellationToken ct = default)
    {
        var fornecedor = await _fornecedores.ObterPorIdAsync(id, rastrear: true, ct)
            ?? throw new NaoEncontradoException("Fornecedor não encontrado.");

        Aplicar(request, fornecedor);
        await _unitOfWork.SalvarAsync(ct);
        return fornecedor;
    }

    public async Task<bool> AtualizarStatusAsync(int id, bool ativo, CancellationToken ct = default)
    {
        var fornecedor = await _fornecedores.ObterPorIdAsync(id, rastrear: true, ct);
        if (fornecedor == null) return false;

        fornecedor.Ativo = ativo;
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    private static void Aplicar(FornecedorRequest request, Fornecedor fornecedor)
    {
        fornecedor.Nome = request.Nome.Trim();
        fornecedor.Cnpj = Mascaras.NormalizarDocumento(request.Cnpj);
        fornecedor.InscricaoEstadual = Mascaras.SomenteDigitos(request.InscricaoEstadual);
        fornecedor.Telefone = Mascaras.NormalizarTelefone(request.Telefone);
        fornecedor.Email = request.Email?.Trim();
    }
}
