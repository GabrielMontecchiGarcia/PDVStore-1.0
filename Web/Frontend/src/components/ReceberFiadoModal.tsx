import { useEffect, useState, type FormEvent } from 'react';
import type { Cliente } from '../models';
import { clienteService } from '../services/cadastroService';
import { useToast } from '../contexts/ToastContext';
import { formatCurrency, mensagemDeErro } from '../utils/format';
import { parseDecimal, toDecimalInput } from '../utils/masks';
import { Modal } from './Modal';

// ============================================================================
// Recebimento de fiado. Regra da tela original: aceita pagamento parcial;
// se o valor informado for maior que o débito, pergunta/registra apenas o
// valor devido (quitando o saldo).
// ============================================================================

interface ReceberFiadoModalProps {
  cliente: Cliente | null;
  onFechar: () => void;
  onRecebido: () => void;
}

export function ReceberFiadoModal({ cliente, onFechar, onRecebido }: ReceberFiadoModalProps) {
  const { sucesso, erro } = useToast();
  const [valorTexto, setValorTexto] = useState('0,00');
  const [confirmarExcedente, setConfirmarExcedente] = useState(false);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!cliente) return;
    setValorTexto(toDecimalInput(cliente.saldoDevedor));
    setConfirmarExcedente(false);
    setErroLocal('');
  }, [cliente]);

  if (!cliente) return null;

  const informado = parseDecimal(valorTexto);
  const excedente = informado > cliente.saldoDevedor;
  const valorEfetivo = excedente ? cliente.saldoDevedor : informado;

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (informado <= 0) {
      setErroLocal('Valor deve ser maior que zero.');
      return;
    }
    if (cliente.saldoDevedor <= 0) {
      setErroLocal('O cliente não possui saldo devedor.');
      return;
    }
    if (excedente && !confirmarExcedente) {
      setErroLocal('Confirme o registro apenas do valor devido para quitar o débito.');
      return;
    }

    setSalvando(true);
    try {
      const atualizado = await clienteService.receberFiado(cliente.id, valorEfetivo);
      sucesso(`Recebimento registrado. Saldo restante: ${formatCurrency(atualizado.saldoDevedor)}.`);
      onRecebido();
      onFechar();
    } catch (e) {
      erro(mensagemDeErro(e));
    } finally {
      setSalvando(false);
    }
  };

  return (
    <Modal
      aberto
      titulo="Receber débito (fiado)"
      subtitulo={`Cliente: ${cliente.nome}`}
      tamanho="sm"
      onFechar={onFechar}
    >
      <form onSubmit={enviar} className="form-stack">
        {erroLocal && <div className="alert alert-danger">{erroLocal}</div>}

        <div className="resumo-fiado">
          <span>Saldo devedor atual</span>
          <strong>{formatCurrency(cliente.saldoDevedor)}</strong>
          <span>Limite de crédito</span>
          <strong>{formatCurrency(cliente.limiteCredito)}</strong>
        </div>

        <label className="field">
          <span>Valor recebido (R$)</span>
          <input inputMode="decimal" value={valorTexto} onChange={(e) => setValorTexto(e.target.value)} />
        </label>

        {excedente && (
          <div className="alert alert-warning">
            <span>
              O valor informado é maior que o débito. Será registrado apenas {formatCurrency(cliente.saldoDevedor)} para quitar o saldo.
            </span>
            <label className="checkbox">
              <input
                type="checkbox"
                checked={confirmarExcedente}
                onChange={(e) => setConfirmarExcedente(e.target.checked)}
              />
              Confirmo registrar apenas o valor devido
            </label>
          </div>
        )}

        <div className="form-actions">
          <button type="button" className="btn btn-ghost" onClick={onFechar} disabled={salvando}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={salvando || (excedente && !confirmarExcedente)}>
            {salvando ? 'Registrando...' : 'Receber'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
