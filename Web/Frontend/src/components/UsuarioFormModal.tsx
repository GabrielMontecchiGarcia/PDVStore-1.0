import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { PERMISSOES, type Usuario } from '../models';
import { usuarioService } from '../services/usuarioService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { urlDeArquivo } from '../utils/download';
import { Modal } from './Modal';
import { Icon } from './Icon';

// ============================================================================
// Formulário de usuário. Regras do frmCadastroUsuario: nome obrigatório e único,
// senha mínima de 6 e confirmação igual; na edição o nome é imutável e a senha
// é sempre redefinida (mesmo comportamento da tela original).
// Inclui a foto do usuário (botão "Escolher Foto" do sistema Windows), enviada
// para a API e gravada em UsuarioCaixa.FotoPath.
// ============================================================================

interface UsuarioFormModalProps {
  aberto: boolean;
  usuario: Usuario | null;
  onFechar: () => void;
  onSalvo: () => void;
}

const TAMANHO_MAXIMO_MB = 2;

export function UsuarioFormModal({ aberto, usuario, onFechar, onSalvo }: UsuarioFormModalProps) {
  const { sucesso, erro } = useToast();
  const [nome, setNome] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [permissao, setPermissao] = useState<number>(1);
  const [fotoPath, setFotoPath] = useState('');
  const [arquivo, setArquivo] = useState<File | null>(null);
  const [previa, setPrevia] = useState<string | null>(null);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!aberto) return;
    setErroLocal('');
    setNome(usuario?.nome ?? '');
    setSenha('');
    setConfirmacao('');
    setPermissao(usuario?.permissaoCodigo ?? 1);
    setFotoPath(usuario?.fotoPath ?? '');
    setArquivo(null);
    setPrevia(null);
  }, [aberto, usuario]);

  // Libera a memória do preview temporário da imagem.
  useEffect(() => () => { if (previa) URL.revokeObjectURL(previa); }, [previa]);

  const escolherFoto = (evento: ChangeEvent<HTMLInputElement>) => {
    const arquivoEscolhido = evento.target.files?.[0];
    if (!arquivoEscolhido) return;

    if (!arquivoEscolhido.type.startsWith('image/')) {
      setErroLocal('Selecione um arquivo de imagem.');
      return;
    }

    if (arquivoEscolhido.size > TAMANHO_MAXIMO_MB * 1024 * 1024) {
      setErroLocal('A imagem deve ter no máximo 2 MB.');
      return;
    }

    setErroLocal('');
    setArquivo(arquivoEscolhido);
    setPrevia(URL.createObjectURL(arquivoEscolhido));
  };

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!usuario && !nome.trim()) { setErroLocal('Nome do usuário é obrigatório!'); return; }
    if (senha.length < 6) { setErroLocal('Senha deve ter no mínimo 6 caracteres!'); return; }
    if (senha !== confirmacao) { setErroLocal('As senhas não coincidem!'); return; }

    setSalvando(true);
    try {
      // A foto é enviada antes do salvamento; sem nova foto, preserva a anterior.
      let caminhoFinal = fotoPath;
      if (arquivo) {
        const enviada = await usuarioService.enviarFoto(arquivo);
        caminhoFinal = enviada.caminho;
      }

      if (usuario) {
        await usuarioService.atualizar(usuario.id, {
          senha,
          confirmacaoSenha: confirmacao,
          permissao,
          fotoPath: caminhoFinal || null
        });
        sucesso('Usuário atualizado com sucesso.');
      } else {
        await usuarioService.criar({
          nome: nome.trim(),
          senha,
          confirmacaoSenha: confirmacao,
          permissao,
          fotoPath: caminhoFinal || null
        });
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

  const fotoAtual = previa ?? urlDeArquivo(fotoPath);

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

        <div className="foto-usuario">
          <span className="foto-preview">
            {fotoAtual ? (
              <img src={fotoAtual} alt={`Foto de ${usuario?.nome ?? 'usuário'}`} />
            ) : (
              <Icon name="userCog" size={26} />
            )}
          </span>

          <div className="foto-acoes">
            <label className="btn btn-outline btn-sm">
              <Icon name="plus" size={14} />
              Escolher foto
              <input type="file" accept="image/png,image/jpeg,image/bmp" onChange={escolherFoto} hidden />
            </label>

            {fotoPath && !previa && (
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setFotoPath('')}>
                Remover foto
              </button>
            )}

            <small className="text-muted">JPG, PNG ou BMP com até {TAMANHO_MAXIMO_MB} MB.</small>
          </div>
        </div>

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
