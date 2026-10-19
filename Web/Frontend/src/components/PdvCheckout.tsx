import { FORMAS_PAGAMENTO, type Cliente, type FormaPagamento } from '../models';
import { formatCurrency } from '../utils/format';

// ============================================================================
// Painel de pagamento do PDV: forma, cliente (fiado), desconto, valor recebido,
// troco e total. A validação final acontece no servidor; aqui espelhamos as
// regras para feedback imediato do operador.
// ============================================================================

interface PdvCheckoutProps {
  formaPagamento: FormaPagamento;
  onFormaPagamento: (forma: FormaPagamento) => void;
  clientes: Cliente[];
  clienteId: string;
  onCliente: (id: string) => void;
  descontoTexto: string;
  onDesconto: (valor: string) => void;
  valorRecebido: string;
  onValorRecebido: (valor: string) => void;
  troco: number;
  subtotal: number;
  desconto: number;
  total: number;
  caixaAberto: boolean;
  caixaCarregando: boolean;
  processando: boolean;
  carrinhoVazio: boolean;
  onFinalizar: () => void;
}

export function PdvCheckout({
  formaPagamento,
  onFormaPagamento,
  clientes,
  clienteId,
  onCliente,
  descontoTexto,
  onDesconto,
  valorRecebido,
  onValorRecebido,
  troco,
  subtotal,
  desconto,
  total,
  caixaAberto,
  caixaCarregando,
  processando,
  carrinhoVazio,
  onFinalizar
}: PdvCheckoutProps) {
  return (
    <aside className="card pdv-checkout">
      <header className="card-header">
        <div>
          <h2>Pagamento</h2>
          <p>Confira o total e finalize a venda.</p>
        </div>
      </header>

      <div className="card-body form-stack">
        <label className="field">
          <span>Forma de pagamento</span>
          <select value={formaPagamento} onChange={(e) => onFormaPagamento(e.target.value as FormaPagamento)}>
            {FORMAS_PAGAMENTO.map((forma) => <option key={forma} value={forma}>{forma}</option>)}
          </select>
        </label>

        <label className="field">
          <span>Cliente {formaPagamento === 'Fiado' ? '(obrigatório para fiado)' : '(opcional)'}</span>
          <select value={clienteId} onChange={(e) => onCliente(e.target.value)}>
            <option value="">Não identificado</option>
            {clientes.map((cliente) => (
              <option key={cliente.id} value={cliente.id}>
                {cliente.nome} · saldo {formatCurrency(cliente.saldoDevedor)} / limite {formatCurrency(cliente.limiteCredito)}
              </option>
            ))}
          </select>
        </label>

        <label className="field">
          <span>Desconto (R$)</span>
          <input type="text" inputMode="decimal" value={descontoTexto} onChange={(e) => onDesconto(e.target.value)} />
        </label>

        {formaPagamento === 'Dinheiro' && (
          <>
            <label className="field">
              <span>Valor recebido (R$)</span>
              <input
                type="text"
                inputMode="decimal"
                value={valorRecebido}
                onChange={(e) => onValorRecebido(e.target.value)}
                placeholder="0,00"
              />
            </label>

            <div className={`troco-box ${troco < 0 ? 'troco-negativo' : ''}`}>
              <span>Troco</span>
              <strong>
                {valorRecebido.trim()
                  ? troco >= 0 ? formatCurrency(troco) : 'Valor insuficiente'
                  : formatCurrency(0)}
              </strong>
            </div>
          </>
        )}

        <div className="total-box">
          <span>Subtotal</span>
          <strong>{formatCurrency(subtotal)}</strong>
          <span>Desconto</span>
          <strong>- {formatCurrency(desconto)}</strong>
          <span className="total-label">Total</span>
          <strong className="total-value">{formatCurrency(total)}</strong>
        </div>

        <button
          type="button"
          className="btn btn-primary btn-block btn-lg"
          onClick={onFinalizar}
          disabled={processando || !caixaAberto || carrinhoVazio || caixaCarregando}
        >
          {processando ? 'Registrando venda...' : 'Finalizar venda'}
        </button>

        {!caixaAberto && !caixaCarregando && (
          <small className="text-muted text-center">É necessário abrir o caixa antes de vender.</small>
        )}
      </div>
    </aside>
  );
}
