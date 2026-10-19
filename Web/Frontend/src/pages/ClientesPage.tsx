import { useState } from 'react';
import { useApi } from '../hooks/useApi';
import { useDebounce, useDocumentTitle } from '../hooks/useDebounce';
import { clienteService } from '../services/cadastroService';
import type { Cliente } from '../models';
import { formatCurrency, mensagemDeErro } from '../utils/format';
import { PageHeader, Loading, EmptyState, AtivoBadge } from '../components/PageHeader';
import { ClienteFormModal } from '../components/ClienteFormModal';
import { ReceberFiadoModal } from '../components/ReceberFiadoModal';
import { ConfirmDialog } from '../components/Modal';
import { ExportButtons } from '../components/ExportButtons';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Clientes e caderneta (fiado). Exclusivo do Administrador.
// Regras: CPF/CNPJ válido, limite mínimo, saldo devedor e desativação lógica.
// ============================================================================

export function ClientesPage() {
  useDocumentTitle('Clientes');
  const { sucesso, erro } = useToast();

  const [busca, setBusca] = useState('');
  const buscaDebounced = useDebounce(busca);
  const [apenasAtivos, setApenasAtivos] = useState(true);
  const [modalAberto, setModalAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<Cliente | null>(null);
  const [recebendo, setRecebendo] = useState<Cliente | null>(null);
  const [paraDesativar, setParaDesativar] = useState<Cliente | null>(null);
  const [desativando, setDesativando] = useState(false);

  const { dados, carregando, erro: erroApi, recarregar } = useApi(
    () => clienteService.listar(apenasAtivos, buscaDebounced),
    [apenasAtivos, buscaDebounced]
  );

  const abrirNovo = () => { setEmEdicao(null); setModalAberto(true); };
  const abrirEdicao = (cliente: Cliente) => { setEmEdicao(cliente); setModalAberto(true); };

  const desativar = async () => {
    if (!paraDesativar) return;
    setDesativando(true);
    try {
      await clienteService.desativar(paraDesativar.id);
      sucesso('Cliente desativado.');
      setParaDesativar(null);
      await recarregar(true);
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setDesativando(false);
    }
  };

  const clientes = dados ?? [];

  return (
    <div className="page">
      <PageHeader
        titulo="Clientes"
        descricao="Cadastro, limite de crédito e controle de fiado (caderneta)."
        acoes={
          <>
            <ExportButtons tipo="clientes" params={{ apenasAtivos }} />
            <button type="button" className="btn btn-primary" onClick={abrirNovo}>
              <Icon name="plus" size={16} />
              Novo cliente
            </button>
          </>
        }
      />

      {erroApi && <div className="alert alert-danger">{erroApi}</div>}

      <section className="card">
        <div className="toolbar">
          <div className="search-box">
            <Icon name="search" size={18} />
            <input
              type="text"
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
              placeholder="Buscar por nome ou CPF/CNPJ"
            />
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
        ) : clientes.length === 0 ? (
          <EmptyState
            icone="users"
            titulo="Nenhum cliente encontrado"
            descricao="Cadastre clientes para controlar vendas fiadas."
            acao={<button className="btn btn-primary" onClick={abrirNovo}>Novo cliente</button>}
          />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Cliente</th>
                  <th>CPF/CNPJ</th>
                  <th>Telefone</th>
                  <th className="text-right">Limite</th>
                  <th className="text-right">Saldo devedor</th>
                  <th>Status</th>
                  <th className="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                {clientes.map((cliente) => (
                  <tr key={cliente.id}>
                    <td>
                      <strong>{cliente.nome}</strong>
                      {cliente.email && <small className="cell-sub">{cliente.email}</small>}
                    </td>
                    <td>{cliente.cpfCnpj || '—'}</td>
                    <td>{cliente.telefone || '—'}</td>
                    <td className="text-right">{formatCurrency(cliente.limiteCredito)}</td>
                    <td className={`text-right ${cliente.saldoDevedor > 0 ? 'text-danger' : ''}`}>
                      <strong>{formatCurrency(cliente.saldoDevedor)}</strong>
                    </td>
                    <td><AtivoBadge ativo={cliente.ativo} /></td>
                    <td className="text-right actions">
                      <button
                        type="button"
                        className="btn btn-outline btn-sm"
                        onClick={() => setRecebendo(cliente)}
                        disabled={cliente.saldoDevedor <= 0}
                        title="Receber débito"
                      >
                        <Icon name="cash" size={14} />
                        Receber
                      </button>
                      <button type="button" className="icon-button" title="Editar" onClick={() => abrirEdicao(cliente)}>
                        <Icon name="edit" size={16} />
                      </button>
                      <button type="button" className="icon-button danger" title="Desativar" onClick={() => setParaDesativar(cliente)}>
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

      <ClienteFormModal
        aberto={modalAberto}
        cliente={emEdicao}
        onFechar={() => setModalAberto(false)}
        onSalvo={() => void recarregar(true)}
      />

      <ReceberFiadoModal
        cliente={recebendo}
        onFechar={() => setRecebendo(null)}
        onRecebido={() => void recarregar(true)}
      />

      <ConfirmDialog
        aberto={!!paraDesativar}
        titulo="Desativar cliente"
        mensagem={`Deseja desativar o cliente "${paraDesativar?.nome}"? Ele não poderá mais receber venda fiada, mas o histórico será preservado.`}
        confirmarLabel="Desativar"
        perigo
        carregando={desativando}
        onConfirmar={() => void desativar()}
        onCancelar={() => setParaDesativar(null)}
      />
    </div>
  );
}
