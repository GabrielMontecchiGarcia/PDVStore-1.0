import { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDebounce, useDocumentTitle } from '../hooks/useDebounce';
import { fornecedorService } from '../services/cadastroService';
import type { Fornecedor } from '../models';
import { mensagemDeErro } from '../utils/format';
import { PageHeader, Loading, EmptyState, AtivoBadge } from '../components/PageHeader';
import { FornecedorFormModal } from '../components/FornecedorFormModal';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Fornecedores (exclusivo do Administrador). Desativação lógica preserva o
// histórico de compras.
// ============================================================================

export function FornecedoresPage() {
  useDocumentTitle('Fornecedores');
  const { sucesso, erro } = useToast();

  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca);
  const [apenasAtivos, setApenasAtivos] = useState(true);
  const [modalAberto, setModalAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<Fornecedor | null>(null);
  const [paraDesativar, setParaDesativar] = useState<Fornecedor | null>(null);
  const [desativando, setDesativando] = useState(false);

  const { dados, carregando, erro: erroApi, recarregar } = useApi(
    () => fornecedorService.listar(apenasAtivos, buscaDebounced),
    [apenasAtivos, buscaDebounced]
  );

  const abrirNovo = () => { setEmEdicao(null); setModalAberto(true); };
  const abrirEdicao = (fornecedor: Fornecedor) => { setEmEdicao(fornecedor); setModalAberto(true); };

  const desativar = async () => {
    if (!paraDesativar) return;
    setDesativando(true);
    try {
      await fornecedorService.desativar(paraDesativar.id);
      sucesso('Fornecedor desativado.');
      setParaDesativar(null);
      await recarregar(true);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setDesativando(false);
    }
  };

  const fornecedores = dados ?? [];

  return (
    <div className="page">
      <PageHeader
        titulo="Fornecedores"
        descricao="Parceiros usados nas compras e entradas de mercadoria."
        acoes={
          <>
            <ExportButtons tipo="fornecedores" params={{ apenasAtivos }} />
            <button type="button" className="btn btn-primary" onClick={abrirNovo}>
              <Icon name="plus" size={16} />
              Novo fornecedor
            </button>
          </>
        }
      />

      {erroApi && <div className="alert alert-danger">{erroApi}</div>}

      <section className="card">
        <div className="toolbar">
          <div className="search-box">
            <Icon name="search" size={18} />
            <input type="text" value={busca} onChange={(e) => setBusca(e.target.value)} placeholder="Buscar por nome ou CNPJ" />
          </div>
          <label className="checkbox">
            <input type="checkbox" checked={!apenasAtivos} onChange={(e) => setApenasAtivos(!e.target.checked)} />
            Mostrar inativos
          </label>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => void recarregar()} disabled={carregando}>
            <Icon name="refresh" size={16} />
            Atualizar
          </button>
        </div>

        {carregando ? (
          <Loading />
        ) : fornecedores.length === 0 ? (
          <EmptyState
            icone="truck"
            titulo="Nenhum fornecedor encontrado"
            descricao="Cadastre fornecedores para registrar compras."
            acao={<button className="btn btn-primary" onClick={abrirNovo}>Novo fornecedor</button>}
          />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Fornecedor</th>
                  <th>CNPJ</th>
                  <th>Inscrição estadual</th>
                  <th>Telefone</th>
                  <th>Status</th>
                  <th className="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                {fornecedores.map((fornecedor) => (
                  <tr key={fornecedor.id}>
                    <td>
                      <strong>{fornecedor.nome}</strong>
                      {fornecedor.email && <small className="cell-sub">{fornecedor.email}</small>}
                    </td>
                    <td>{fornecedor.cnpj || '—'}</td>
                    <td>{fornecedor.inscricaoEstadual || '—'}</td>
                    <td>{fornecedor.telefone || '—'}</td>
                    <td><AtivoBadge ativo={fornecedor.ativo} /></td>
                    <td className="text-right actions">
                      <button type="button" className="icon-button" title="Editar" onClick={() => abrirEdicao(fornecedor)}>
                        <Icon name="edit" size={16} />
                      </button>
                      <button type="button" className="icon-button danger" title="Desativar" onClick={() => setParaDesativar(fornecedor)}>
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

      <FornecedorFormModal
        aberto={modalAberto}
        fornecedor={emEdicao}
        onFechar={() => setModalAberto(false)}
        onSalvo={() => void recarregar(true)}
      />

      <ConfirmDialog
        aberto={!!paraDesativar}
        titulo="Desativar fornecedor"
        mensagem={`Deseja desativar o fornecedor "${paraDesativar?.nome}"? O histórico de compras será preservado.`}
        confirmarLabel="Desativar"
        perigo
        carregando={desativando}
        onConfirmar={() => void desativar()}
        onCancelar={() => setParaDesativar(null)}
      />
    </div>
  );
}
