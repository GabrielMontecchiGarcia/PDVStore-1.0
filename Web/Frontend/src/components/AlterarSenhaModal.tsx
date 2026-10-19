import { useState, type FormEvent } from 'react';
import { authService } from '../services/authService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { Modal } from './Modal';

// ============================================================================
// Modal de troca de senha do próprio usuário (mesma regra do serviço original:
// exige a senha atual correta e nova senha com no mínimo 6 caracteres).
// ============================================================================

export function AlterarSenhaModal({ aberto, onFechar }: { aberto: boolean; onFechar: () => void }) {
  const { sucesso, erro } = useToast();
  const [senhaAtual, setSenhaAtual] = useState('');
  const [novaSenha, setNovaSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erroLocal, setErroLocal] = useState('');

  const limpar = () => {
    setSenhaAtual('');
    setNovaSenha('');
    setConfirmacao('');
    setErroLocal('');
  };

  const fechar = () => {
    if (salvando) return;
    limpar();
    onFechar();
  };

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!senhaAtual.trim()) {
      setErroLocal('Informe a senha atual.');
      return;
    }
    if (novaSenha.length < 6) {
      setErroLocal('Senha deve ter no mínimo 6 caracteres!');
      return;
    }
    if (novaSenha !== confirmacao) {
      setErroLocal('As senhas não coincidem!');
      return;
    }

    setSalvando(true);
    try {
      await authService.alterarSenha(senhaAtual, novaSenha);
      sucesso('Senha alterada com sucesso.');
      limpar();
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
      titulo="Alterar senha"
      subtitulo="Informe a senha atual e defina a nova senha de acesso."
      tamanho="sm"
      onFechar={fechar}
    >
      <form onSubmit={enviar} className="form-stack">
        {erroLocal && <div className="alert alert-danger">{erroLocal}</div>}

        <label className="field">
          <span>Senha atual</span>
          <input
            type="password"
            value={senhaAtual}
            onChange={(e) => setSenhaAtual(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>

        <label className="field">
          <span>Nova senha</span>
          <input
            type="password"
            value={novaSenha}
            onChange={(e) => setNovaSenha(e.target.value)}
            autoComplete="new-password"
            minLength={6}
            required
          />
        </label>

        <label className="field">
          <span>Confirmar nova senha</span>
          <input
            type="password"
            value={confirmacao}
            onChange={(e) => setConfirmacao(e.target.value)}
            autoComplete="new-password"
            required
          />
        </label>

        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={fechar} disabled={salvando}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar nova senha'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
