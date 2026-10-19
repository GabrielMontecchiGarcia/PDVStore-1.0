import { useState, type FormEvent } from 'react';
import { useApi } from '../hooks/useApi';
import { useDocumentTitle } from '../hooks/useDebounce';
import { caixaService } from '../services/caixaService';
import { formatCurrency, formatDateTime, mensagemDeErro } from '../utils/format';
import { parseDecimal } from '../utils/masks';
import { PageHeader, Loading, EmptyState, StatusBadge } from '../components/PageHeader';
import { Modal, ConfirmDialog } from '../components/Modal';
import { Icon } from '../components/Icon';
import { useToast } from '../contexts/ToastContext';

// ============================================================================
// Caixa (exclusivo do Administrador): abertura com fundo de troco, sangria,
// fechamento com valor calculado e histórico. Regra: apenas um caixa aberto.
// ============================================================================

export function CaixaPage() {
  useDocumentTitle('Caixa');
  const { sucesso, erro } = useToast();

  const atualApi = useApi(() => caixaService.obterAberto(), []);
  const listaApi = useApi(() => caixaService.listar(), []);

  const [modalAbrir, setModalAbrir] = useState(false);
  const [modalSangria, setModalSangria] = useState(false);
  const [confirmarFechamento, setConfirmarFechamento] = useState(false);
  const [valorInicial, setValorInicial] = useState('0,00');
  const [valorSangria, setValorSangria] = useState('0,00');
  const [processando, setProcessando] = useState(false);

  const caixa = atualApi.dados;
  const caixas = listaApi.dados ?? [];

  const atualizarTudo = async () => {
    await Promise.all([atualApi.recarregar(true), listaApi.recarregar(true)]);
  };

  const abrir = async (evento: FormEvent) => {
    evento.preventDefault();
    setProcessando(true);
    try {
      const criado = await caixaService.abrir(parseDecimal(valorInicial));
      sucesso(`Caixa #${criado.id} aberto com ${formatCurrency(criado.valorInicial)}.`);
      setModalAbrir(false);
      setValorInicial('0,00');
      await atualizarTudo();
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setProcessando(false);
    }
  };

  const sangria = async (evento: FormEvent) => {
    evento.preventDefault();
    if (!caixa) return;

    const valor = parseDecimal(valorSangria);
    if (valor <= 0) { erro('Valor da sangria deve ser maior que zero.'); return; }

    setProcessando(true);
    try {
      await caixaService.sangria(caixa.id, valor);
      sucesso(`Sangria de ${formatCurrency(valor)} registrada.`);
      setModalSangria(false);
      setValorSangria('0,00');
      await atualizarTudo();
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setProcessando(false);
    }
  };

  const fechar = async () => {
    if (!caixa) return;
    setProcessando(true);
    try {
      await caixaService.fechar(caixa.id);
      setConfirmarFechamento(false);
      await atualizarTudo();
      sucesso('Caixa fechado com sucesso.');
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setProcessando(false);
    }
  };

  return (
    <div className="page">
      <PageHeader titulo="Abrir / Fechar Caixa" descricao="Controle do turno, fundo de troco, sangrias e fechamento." />

      <section className={`card caixa-status ${caixa ? 'caixa-aberto' : 'caixa-fechado'}`}>
        <div className="caixa-status-icon">
          <Icon name="cash" size={26} />
        </div>
        <div className="caixa-status-info">
          {atualApi.carregando ? (
            <p>Verificando caixa...</p>
          ) : caixa ? (
            <>
              <div className="caixa-titulo">
                <h2>Caixa #{caixa.id} aberto</h2>
                <StatusBadge status={caixa.status} />
              </div>
              <p>
                Aberto em {formatDateTime(caixa.abertura)} por {caixa.usuarioNome ?? '—'}
              </p>
              <div className="caixa-valores">
                <span>Valor inicial <strong>{formatCurrency(caixa.valorInicial)}</strong></span>
                <span>Sangria <strong>{formatCurrency(caixa.sangria ?? 0)}</strong></span>
              </div>
            </>
          ) : (
            <>
              <div className="caixa-titulo">
                <h2>Caixa fechado</h2>
                <StatusBadge status="Fechado" />
              </div>
              <p>Abra o caixa para liberar as vendas no PDV.</p>
            </>
          )}
        </div>
        <div className="caixa-acoes">
          {caixa ? (
            <>
              <button type="button" className="btn btn-outline" onClick={() => setModalSangria(true)}>
                <Icon name="minus" size={16} /> Sangria
              </button>
              <button type="button" className="btn btn-danger" onClick={() => setConfirmarFechamento(true)}>
                Fechar caixa
              </button>
            </>
          ) : (
            <button type="button" className="btn btn-primary" onClick={() => setModalAbrir(true)} disabled={atualApi.carregando}>
              <Icon name="plus" size={16} /> Abrir caixa
            </button>
          )}
        </div>
      </section>

      <section className="card">
        <header className="card-header">
          <div><h2>Histórico de caixas</h2><p>Turnos abertos e fechados, do mais recente ao mais antigo.</p></div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => void atualizarTudo()} disabled={listaApi.carregando}>
            <Icon name="refresh" size={16} /> Atualizar
          </button>
        </header>

        {listaApi.carregando ? <Loading /> : caixas.length === 0 ? (
          <EmptyState icone="cash" titulo="Nenhum caixa registrado" />
        ) : (
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th><th>Abertura</th><th>Fechamento</th><th>Usuário</th>
                  <th className="text-right">Inicial</th><th className="text-right">Sangria</th>
                  <th className="text-right">Valor final</th><th>Status</th>
                </tr>
              </thead>
              <tbody>
                {caixas.map((item) => (
                  <tr key={item.id}>
                    <td>{item.id}</td>
                    <td>{formatDateTime(item.abertura)}</td>
                    <td>{item.fechamento ? formatDateTime(item.fechamento) : '—'}</td>
                    <td>{item.usuarioNome ?? '—'}</td>
                    <td className="text-right">{formatCurrency(item.valorInicial)}</td>
                    <td className="text-right">{formatCurrency(item.sangria ?? 0)}</td>
                    <td className="text-right"><strong>{item.valorFinal != null ? formatCurrency(item.valorFinal) : '—'}</strong></td>
                    <td><StatusBadge status={item.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <Modal
        aberto={modalAbrir}
        titulo="Abrir caixa"
        subtitulo="Informe o valor inicial (fundo de troco)."
        tamanho="sm"
        onFechar={() => setModalAbrir(false)}
      >
        <form onSubmit={abrir} className="form-stack">
          <label className="field">
            <span>Valor inicial (R$)</span>
            <input inputMode="decimal" value={valorInicial} onChange={(e) => setValorInicial(e.target.value)} />
          </label>
          <div className="form-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setModalAbrir(false)} disabled={processando}>Cancelar</button>
            <button type="submit" className="btn btn-primary" disabled={processando}>Abrir caixa</button>
          </div>
        </form>
      </Modal>

      <Modal
        aberto={modalSangria}
        titulo="Registrar sangria"
        subtitulo={caixa ? `Caixa #${caixa.id} · saldo inicial ${formatCurrency(caixa.valorInicial)}` : ''}
        tamanho="sm"
        onFechar={() => setModalSangria(false)}
      >
        <form onSubmit={sangria} className="form-stack">
          <label className="field">
            <span>Valor a retirar (R$)</span>
            <input inputMode="decimal" value={valorSangria} onChange={(e) => setValorSangria(e.target.value)} />
          </label>
          <p className="text-muted">A sangria é acumulada e descontada no valor final do fechamento.</p>
          <div className="form-actions">
            <button type="button" className="btn btn-ghost" onClick={() => setModalSangria(false)} disabled={processando}>Cancelar</button>
            <button type="submit" className="btn btn-primary" disabled={processando}>Registrar sangria</button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        aberto={confirmarFechamento}
        titulo="Fechar caixa"
        mensagem={`Fechar o caixa #${caixa?.id ?? ''}? O valor final será calculado como inicial + vendas concluídas - sangrias.`}
        confirmarLabel="Fechar caixa"
        perigo
        carregando={processando}
        onConfirmar={() => void fechar()}
        onCancelar={() => setConfirmarFechamento(false)}
      />
    </div>
  );
}
