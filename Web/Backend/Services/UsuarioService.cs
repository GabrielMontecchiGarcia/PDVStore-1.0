using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de usuários. Replica as regras do UsuarioService original:
/// - Nome obrigatório e único;
/// - Senha convertida em hash BCrypt, nunca gravada em texto puro;
/// - Edição não altera o nome (imutável) e sempre redefine a senha, como a tela;
/// - Exclusão é lógica (Ativo = false);
/// - Autenticação não revela qual dado está errado.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(IUsuarioRepository usuarios, IUnitOfWork unitOfWork)
    {
        _usuarios = usuarios;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Cria um usuário com senha hasheada, data UTC e ativo. O nome duplicado é
    /// bloqueado com a mesma mensagem exibida pela tela de cadastro.
    /// </summary>
    public async Task<UsuarioCaixa> CriarAsync(CriarUsuarioRequest request, CancellationToken ct = default)
    {
        var nome = request.Nome.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("Nome é obrigatório.");

        if (await _usuarios.ExisteNomeAsync(nome, ct))
            throw new RegraDeNegocioException("Este nome de usuário já está em uso!");

        var usuario = new UsuarioCaixa
        {
            Nome = nome,
            Permissao = (TipoPermissao)request.Permissao,
            FotoPath = request.FotoPath
        };

        usuario.SetSenha(request.Senha);
        usuario.CreatedAt = DateTime.UtcNow;
        usuario.SetAtivo(true);

        await _usuarios.AdicionarAsync(usuario, ct);
        await _unitOfWork.SalvarAsync(ct);

        return usuario;
    }

    /// <summary>
    /// Atualiza permissão, foto e senha de um usuário existente. O nome é mantido
    /// (campo desabilitado na tela original). Sem foto nova, preserva a anterior.
    /// </summary>
    public async Task<UsuarioCaixa> AtualizarAsync(int id, AtualizarUsuarioRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id, rastrear: true, ct)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");

        usuario.Permissao = (TipoPermissao)request.Permissao;
        usuario.SetSenha(request.Senha);

        if (request.FotoPath is not null)
            usuario.SetFotoPath(request.FotoPath);

        await _unitOfWork.SalvarAsync(ct);
        return usuario;
    }

    /// <summary>Desativa o usuário (soft delete), preservando o histórico.</summary>
    public async Task DesativarAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id, rastrear: true, ct)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");

        usuario.SetAtivo(false);
        await _unitOfWork.SalvarAsync(ct);
    }

    public Task<List<UsuarioCaixa>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default) =>
        _usuarios.ListarAsync(apenasAtivos, filtro?.Trim(), ct);

    public Task<UsuarioCaixa?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _usuarios.ObterPorIdAsync(id, rastrear: false, ct);

    /// <summary>
    /// Autentica por nome + senha. Retorna o usuário mesmo inativo para que a API
    /// consiga exibir a mensagem específica da tela ("Usuário inativo...").
    /// Retorna null apenas para credenciais inválidas.
    /// </summary>
    public async Task<UsuarioCaixa?> AutenticarAsync(string nome, string senha, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObterPorNomeAsync(nome.Trim(), ct);
        if (usuario == null || !usuario.Autenticar(senha))
            return null;

        return usuario;
    }

    /// <summary>
    /// Troca a senha somente se a senha atual conferir. Mantém a regra original de
    /// retornar false (sem alterar nada) em qualquer falha.
    /// </summary>
    public async Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id, rastrear: true, ct);
        if (usuario == null || !usuario.Autenticar(senhaAtual))
            return false;

        usuario.SetSenha(novaSenha);
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    public async Task<bool> EhAdministradorAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObterPorIdAsync(id, rastrear: false, ct);
        return usuario?.EhAdmin() == true;
    }
}
