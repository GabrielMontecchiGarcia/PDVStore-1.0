import { Modal } from './Modal';
import { formatCurrency, formatDateTime } from '../utils/format';
import type { Venda } from '../models';
import { Icon } from './Icon';

// ============================================================================
// Recibo da venda (equivalente ao cupom gerado/impresso pelo frmPDV).
// O texto inclui itens, desconto, total, forma de pagamento, troco, PIX e,
// para fiado, linha de assinatura do cliente.
// ============================================================================

interface ReciboModalProps {
  venda: Venda | null;
  valorRecebido?: number;
  onFechar: () => void;
}

const gerarTexto = (venda: Venda, valorRecebido?: number): string => {
  const linhas: string[] = [];
  linhas.push('           PDVStore - CUPOM NÃO FISCAL');
  linhas.push('------------------------------------------');
  linhas.push(`Venda: #${venda.id}`);
  linhas.push(`Data: ${formatDateTime(venda.dataVenda)}`);
  linhas.push(`Operador: ${venda.usuarioNome ?? '-'}`);
  linhas.push(`Cliente: ${venda.clienteNome ?? 'Não identificado'}`);
  linhas.push('------------------------------------------');

  for (const item of venda.itens) {
    linhas.push(item.produtoNome ?? `Produto #${item.produtoId}`);
    linhas.push(
      `  ${item.quantidade} x ${formatCurrency(item.precoUnitario)}   ${formatCurrency(item.subtotal)}`
    );
  }

  linhas.push('------------------------------------------');
  if (venda.desconto > 0) linhas.push(`Desconto: ${formatCurrency(venda.desconto)}`);
  linhas.push(`TOTAL: ${formatCurrency(venda.valorTotal)}`);
  linhas.push(`Forma de pagamento: ${venda.formaPagamento}`);

  if (valorRecebido !== undefined) {
    const troco = valorRecebido - venda.valorTotal;
    linhas.push(`Valor recebido: ${formatCurrency(valorRecebido)}`);
    if (troco > 0) linhas.push(`Troco: ${formatCurrency(troco)}`);
  }

  if (venda.pixTxId) linhas.push(`TxId PIX: ${venda.pixTxId}`);

  if (venda.formaPagamento === 'Fiado') {
    linhas.push('');
    linhas.push('Assinatura do cliente: __________________________');
  }

  linhas.push('');
  linhas.push('        Obrigado pela preferência!');
  return linhas.join('\n');
};

export function ReciboModal({ venda, valorRecebido, onFechar }: ReciboModalProps) {
  if (!venda) return null;

  return (
    <Modal
      aberto
      titulo={`Venda #${venda.id} registrada`}
      subtitulo="Recibo gerado com sucesso. Você pode imprimir ou fechar."
      tamanho="sm"
      onFechar={onFechar}
      footer={
        <>
          <button type="button" className="btn btn-ghost" onClick={onFechar}>Fechar</button>
          <button type="button" className="btn btn-primary" onClick={() => window.print()}>
            <Icon name="print" size={16} />
            Imprimir recibo
          </button>
        </>
      }
    >
      <pre className="recibo-print recibo-texto">{gerarTexto(venda, valorRecebido)}</pre>
    </Modal>
  );
}
