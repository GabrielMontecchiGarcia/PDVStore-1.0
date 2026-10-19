using FluentValidation;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Application.Validators;

/// <summary>
/// Validação server-side do cadastro de usuário. Mensagens idênticas às da tela
/// frmCadastroUsuario: nome obrigatório, senha mínima de 6 e confirmação igual.
/// </summary>
public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioRequest>
{
    public CriarUsuarioValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do usuário é obrigatório!");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha deve ter no mínimo 6 caracteres!")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres!");

        RuleFor(x => x.ConfirmacaoSenha)
            .Must((request, confirmacao) => confirmacao == request.Senha)
            .WithMessage("As senhas não coincidem!");

        RuleFor(x => x.Permissao)
            .Must(p => Enum.IsDefined(typeof(TipoPermissao), p))
            .WithMessage("Perfil de permissão inválido.");
    }
}

/// <summary>
/// Validação da edição de usuário. O nome é imutável (como na tela original);
/// a senha é sempre redefinida, exigindo mínimo de 6 caracteres.
/// </summary>
public class AtualizarUsuarioValidator : AbstractValidator<AtualizarUsuarioRequest>
{
    public AtualizarUsuarioValidator()
    {
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha deve ter no mínimo 6 caracteres!")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres!");

        RuleFor(x => x.ConfirmacaoSenha)
            .Must((request, confirmacao) => confirmacao == request.Senha)
            .WithMessage("As senhas não coincidem!");

        RuleFor(x => x.Permissao)
            .Must(p => Enum.IsDefined(typeof(TipoPermissao), p))
            .WithMessage("Perfil de permissão inválido.");
    }
}

/// <summary>Login exige usuário e senha preenchidos (mensagem da tela de login).</summary>
public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Nome) && !string.IsNullOrWhiteSpace(x.Senha))
            .WithMessage("Informe usuário e senha.");
    }
}

/// <summary>Troca de senha: nova senha mínima de 6 caracteres.</summary>
public class AlterarSenhaValidator : AbstractValidator<AlterarSenhaRequest>
{
    public AlterarSenhaValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(x => x.NovaSenha)
            .NotEmpty().WithMessage("Senha deve ter no mínimo 6 caracteres!")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres!");
    }
}
