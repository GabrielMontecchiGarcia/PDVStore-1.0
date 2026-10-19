import { useEffect, useState, type FormEvent } from 'react';
import type { Cliente, ClienteRequest } from '../models';
import { clienteService } from '../services/cadastroService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import {
  formatCpfCnpj, formatPhone, onlyDigits, parseDecimal, toDecimalInput,
  validarCpf, validarCnpj, validarEmail
} from '../utils/masks';
import { Modal } from './Modal';

// ============================================================================
// Formulário de cliente (criar/editar) com as mesmas validações da tela
// frmClientes: nome, CPF/CNPJ válido, telefone com DDD, e-mail, endereço e
// limite de crédito >= 10.
// ============================================================================

interface ClienteFormModalProps {
  aberto: boolean;
  cliente: Cliente | null;
  onFechar: () => void;
  onSalvo: () => void;
}

interface CamposCliente {
  nome: string;
  cpfCnpj: string;
  telefone: string;
  email: string;
  endereco: string;
  limiteCredito: string;
}

const vazio: CamposCliente = {
  nome: '', cpfCnpj: '', telefone: '', email: '', endereco: '', limiteCredito: '0,00'
};

export function ClienteFormModal({ aberto, cliente, onFechar, onSalvo }: ClienteFormModalProps) {
  const { sucesso, erro } = useToast();
  const [campos, setCampos] = useState<CamposCliente>(vazio);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!aberto) return;
    setErroLocal('');
    setCampos(cliente ? {
      nome: cliente.nome,
      cpfCnpj: cliente.cpfCnpj ?? '',
      telefone: cliente.telefone ?? '',
      email: cliente.email ?? '',
      endereco: cliente.endereco ?? '',
      limiteCredito: toDecimalInput(cliente.limiteCredito)
    } : vazio);
  }, [aberto, cliente]);

  const alterar = (campo: keyof CamposCliente, valor: string) =>
    setCampos((atuais) => ({ ...atuais, [campo]: valor }));

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!campos.nome.trim()) { setErroLocal('Nome do cliente é obrigatório.'); return; }

    const doc = onlyDigits(campos.cpfCnpj);
    const docValido = doc.length === 11 ? validarCpf(doc) : doc.length === 14 ? validarCnpj(doc) : false;
    if (!docValido) { setErroLocal('Informe um CPF ou CNPJ válido.'); return; }

    if (onlyDigits(campos.telefone).length < 10) {
      setErroLocal('Informe um telefone válido com DDD (mínimo 10 dígitos).');
      return;
    }

    if (!validarEmail(campos.email)) { setErroLocal('Informe um e-mail válido.'); return; }

    if (!campos.endereco.trim()) { setErroLocal('Informe o endereço do cliente.'); return; }

    const limite = parseDecimal(campos.limiteCredito);
    if (limite < 10) {
      setErroLocal('Informe um limite de crédito válido (número positivo e maior ou igual a 10).');
      return;
    }

    const payload: ClienteRequest = {
      nome: campos.nome.trim(),
      cpfCnpj: formatCpfCnpj(campos.cpfCnpj),
      telefone: formatPhone(campos.telefone),
      email: campos.email.trim(),
      endereco: campos.endereco.trim(),
      limiteCredito: limite
    };

    setSalvando(true);
    try {
      if (cliente) {
        await clienteService.atualizar(cliente.id, payload);
        sucesso('Cliente atualizado com sucesso.');
      } else {
        await clienteService.criar(payload);
        sucesso(`Cliente salvo com sucesso! Limite de crédito: ${payload.limiteCredito.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}`);
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
      titulo={cliente ? `Editar cliente #${cliente.id}` : 'Novo cliente'}
      subtitulo="Dados cadastrais e limite de crédito para venda fiada."
      tamanho="lg"
      onFechar={onFechar}
    >
      <form onSubmit={enviar} className="form-grid">
        {erroLocal && <div className="alert alert-danger span-2">{erroLocal}</div>}

        <label className="field span-2">
          <span>Nome *</span>
          <input value={campos.nome} onChange={(e) => alterar('nome', e.target.value)} />
        </label>

        <label className="field">
          <span>CPF/CNPJ *</span>
          <input
            value={campos.cpfCnpj}
            onChange={(e) => alterar('cpfCnpj', formatCpfCnpj(e.target.value))}
            placeholder="000.000.000-00"
          />
        </label>

        <label className="field">
          <span>Telefone *</span>
          <input
            value={campos.telefone}
            onChange={(e) => alterar('telefone', formatPhone(e.target.value))}
            placeholder="(00) 00000-0000"
          />
        </label>

        <label className="field">
          <span>E-mail *</span>
          <input type="email" value={campos.email} onChange={(e) => alterar('email', e.target.value)} />
        </label>

        <label className="field">
          <span>Limite de crédito (R$) *</span>
          <input inputMode="decimal" value={campos.limiteCredito} onChange={(e) => alterar('limiteCredito', e.target.value)} />
        </label>

        <label className="field span-2">
          <span>Endereço *</span>
          <input value={campos.endereco} onChange={(e) => alterar('endereco', e.target.value)} />
        </label>

        <div className="form-actions span-2">
          <button type="button" className="btn btn-ghost" onClick={onFechar} disabled={salvando}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar cliente'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
