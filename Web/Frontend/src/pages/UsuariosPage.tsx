import { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDebounce, useDocumentTitle } from '../hooks/useDebounce';
import { usuarioService } from '../services/usuarioService';
import type { Usuario } from '../models';
import { formatDateTime, mensagemDeErro } from '../utils/format';
import { PageHeader, Loading, EmptyState, AtivoBadge } from '../components/PageHeader';
import { UsuarioFormModal } from '../components/UsuarioFormModal';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Gerenciamento de usuários (exclusivo do Administrador). A exclusão é lógica
// (desativação), preservando o histórico de operações.
// ============================================================================

export function UsuariosPage() {
  useDocumentTitle('Usuários');
  const { sucesso, erro } = useToast();

  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca);
  const [modalAberto, setModalAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<Usuario | null>(null);
  const [paraDesativar, setParaDesativar] = useState<Usuario | null>(null);
  const [desativando, setDesativando] = useState(false);

  const { dados, carregando, erro: erroApi, recarregar } = useApi(
    () => usuarioService.listar(false, buscaDebounced),
    [buscaDebounced]
  );

  const abrirNovo = () => { setEmEdicao(null); setModalAberto(true); };
  const abrirEdicao = (usuario: Usuario) => { setEmEdicao(usuario); setModalAberto(true); };

  const desativar = async () => {
    if (!paraDesativar) return;
    setDesativando(true);
    try {
      await usuarioService.desativar(paraDesativar.id);
      sucesso('Usuário desativado.');
      setParaDesativar(null);
      await recarregar(true);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setDesativando(false);
    }
  };

  const usuarios = dados ?? [];

  return (
    <div className="page">
      <PageHeader
        titulo="Usuários"
        descricao="Contas, perfis de acesso e situação dos operadores."
        acoes={
          <>
            <ExportButtons tipo="usuarios" />
            <button type="button" className="btn btn-primary" onClick={abrirNovo}>
              <Icon name="plus" size={16} /> Novo usuário
            </button>
          </>
        }
      />

      {erroApi && <div className="alert alert-danger">{erroApi}</div>}

      <section className="card">
        <div className="toolbar">
          <div className="search-box">
            <Icon name="search" size={18} />
            <input value={busca} onChange={(e) => setBusca(e.target.value)} placeholder="Buscar por nome" />
          </div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => void recarregar()} disabled={carregando}>
            <Icon name="refresh" size={16} /> Atualizar
          </button>
        </div>

        {carregando ? <Loading /> : usuarios.length === 0 ? (
          <EmptyState icone="userCog" titulo="Nenhum usuário encontrado" descricao="Cadastre o primeiro usuário." />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Usuário</th><th>Perfil</th><th>Criado em</th><th>Situação</th><th className="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                {usuarios.map((usuario) => (
                  <tr key={usuario.id}>
                    <td><strong>{usuario.nome}</strong><small className="cell-sub">#{usuario.id}</small></td>
                    <td>{usuario.permissaoDescricao}</td>
                    <td>{formatDateTime(usuario.createdAt)}</td>
                    <td><AtivoBadge ativo={usuario.ativo} /></td>
                    <td className="text-right actions">
                      <button type="button" className="icon-button" title="Editar" onClick={() => abrirEdicao(usuario)}>
                        <Icon name="edit" size={16} />
                      </button>
                      <button
                        type="button"
                        className="icon-button danger"
                        title="Desativar"
                        onClick={() => setParaDesativar(usuario)}
                        disabled={!usuario.ativo}
                      >
                        <Icon name="trash" size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <UsuarioFormModal
        aberto={modalAberto}
        usuario={emEdicao}
        onFechar={() => setModalAberto(false)}
        onSalvo={() => void recarregar(true)}
      />

      <ConfirmDialog
        aberto={!!paraDesativar}
        titulo="Desativar usuário"
        mensagem={`Deseja desativar o usuário "${paraDesativar?.nome}"? Ele não conseguirá mais acessar o sistema.`}
        confirmarLabel="Desativar"
        perigo
        carregando={desativando}
        onConfirmar={() => void desativar()}
        onCancelar={() => setParaDesativar(null)}
      />
    </div>
  );
}
