import { useEffect, useState, type FormEvent } from 'react';
import { PERMISSOES, type Usuario } from '../models';
import { usuarioService } from '../services/usuarioService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { Modal } from './Modal';

// ============================================================================
// Formulário de usuário. Regras do frmCadastroUsuario: nome obrigatório e único,
// senha mínima de 6 e confirmação igual; na edição o nome é imutável e a senha
// é sempre redefinida (mesmo comportamento da tela original).
// ============================================================================

interface UsuarioFormModalProps {
  aberto: boolean;
  usuario: Usuario | null;
  onFechar: () => void;
  onSalvo: () => void;
}

export function UsuarioFormModal({ aberto, usuario, onFechar, onSalvo }: UsuarioFormModalProps) {
  const { sucesso, erro } = useToast();
  const [nome, setNome] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [permissao, setPermissao] = useState<number>(1);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!aberto) return;
    setErroLocal('');
    setNome(usuario?.nome ?? '');
    setSenha('');
    setConfirmacao('');
    setPermissao(usuario?.permissaoCodigo ?? 1);
  }, [aberto, usuario]);

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!usuario && !nome.trim()) { setErroLocal('Nome do usuário é obrigatório!'); return; }
    if (senha.length < 6) { setErroLocal('Senha deve ter no mínimo 6 caracteres!'); return; }
    if (senha !== confirmacao) { setErroLocal('As senhas não coincidem!'); return; }

    setSalvando(true);
    try {
      if (usuario) {
        await usuarioService.atualizar(usuario.id, { senha, confirmacaoSenha: confirmacao, permissao });
        sucesso('Usuário atualizado com sucesso.');
      } else {
        await usuarioService.criar({ nome: nome.trim(), senha, confirmacaoSenha: confirmacao, permissao });
        sucesso('Usuário salvo com sucesso.');
      }
      onSalvo();
      onFechar();
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setSalvando(false);
    }
  };

  return (
    <Modal
      aberto={aberto}
      titulo={usuario ? `Editar usuário - ${usuario.nome}` : 'Cadastrar novo usuário'}
      subtitulo="Defina os dados de acesso e o perfil de permissão."
      tamanho="md"
      onFechar={onFechar}
    >
      <form onSubmit={enviar} className="form-stack">
        {erroLocal && <div className="alert alert-danger">{erroLocal}</div>}

        <label className="field">
          <span>Nome de usuário *</span>
          <input value={nome} onChange={(e) => setNome(e.target.value)} disabled={!!usuario} autoComplete="off" />
          {usuario && <small className="text-muted">O nome de usuário não pode ser alterado.</small>}
        </label>

        <label className="field">
          <span>{usuario ? 'Nova senha *' : 'Senha *'}</span>
          <input type="password" value={senha} onChange={(e) => setSenha(e.target.value)} autoComplete="new-password" />
        </label>

        <label className="field">
          <span>Confirmar senha *</span>
          <input type="password" value={confirmacao} onChange={(e) => setConfirmacao(e.target.value)} autoComplete="new-password" />
        </label>

        <label className="field">
          <span>Perfil de permissão *</span>
          <select value={permissao} onChange={(e) => setPermissao(Number(e.target.value))}>
            {PERMISSOES.map((opcao) => (
              <option key={opcao.codigo} value={opcao.codigo}>{opcao.descricao}</option>
            ))}
          </select>
        </label>

        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onFechar} disabled={salvando}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar usuário'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
