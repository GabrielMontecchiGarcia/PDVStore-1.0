import { useEffect, useState, type FormEvent } from 'react';
import type { Fornecedor, FornecedorRequest } from '../models';
import { fornecedorService } from '../services/cadastroService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { formatCpfCnpj, formatPhone, onlyDigits, validarCnpj, validarEmail, validarInscricaoEstadual } from '../utils/masks';
import { Modal } from './Modal';

// ============================================================================
// Formulário de fornecedor com as mesmas regras da tela frmFornecedores:
// nome, CNPJ válido, inscrição estadual válida (14 dígitos), telefone e e-mail.
// ============================================================================

interface FornecedorFormModalProps {
  aberto: boolean;
  fornecedor: Fornecedor | null;
  onFechar: () => void;
  onSalvo: () => void;
}

interface CamposFornecedor {
  nome: string;
  cnpj: string;
  inscricaoEstadual: string;
  telefone: string;
  email: string;
}

const vazio: CamposFornecedor = { nome: '', cnpj: '', inscricaoEstadual: '', telefone: '', email: '' };

export function FornecedorFormModal({ aberto, fornecedor, onFechar, onSalvo }: FornecedorFormModalProps) {
  const { sucesso, erro } = useToast();
  const [campos, setCampos] = useState<CamposFornecedor>(vazio);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!aberto) return;
    setErroLocal('');
    setCampos(fornecedor ? {
      nome: fornecedor.nome,
      cnpj: fornecedor.cnpj ?? '',
      inscricaoEstadual: fornecedor.inscricaoEstadual ?? '',
      telefone: fornecedor.telefone ?? '',
      email: fornecedor.email ?? ''
    } : vazio);
  }, [aberto, fornecedor]);

  const alterar = (campo: keyof CamposFornecedor, valor: string) =>
    setCampos((atuais) => ({ ...atuais, [campo]: valor }));

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!campos.nome.trim()) { setErroLocal('Nome do fornecedor é obrigatório.'); return; }
    if (onlyDigits(campos.cnpj).length !== 14 || !validarCnpj(campos.cnpj)) {
      setErroLocal('Informe um CNPJ válido.'); return;
    }
    if (onlyDigits(campos.telefone).length < 10) {
      setErroLocal('Informe um telefone válido com DDD (mínimo 10 dígitos).'); return;
    }
    if (!validarEmail(campos.email)) { setErroLocal('Informe um e-mail válido.'); return; }
    if (onlyDigits(campos.inscricaoEstadual).length !== 14 || !validarInscricaoEstadual(campos.inscricaoEstadual)) {
      setErroLocal('Informe uma inscrição estadual válida.'); return;
    }

    const payload: FornecedorRequest = {
      nome: campos.nome.trim(),
      cnpj: formatCpfCnpj(campos.cnpj),
      inscricaoEstadual: onlyDigits(campos.inscricaoEstadual),
      telefone: formatPhone(campos.telefone),
      email: campos.email.trim()
    };

    setSalvando(true);
    try {
      if (fornecedor) {
        await fornecedorService.atualizar(fornecedor.id, payload);
        sucesso('Fornecedor atualizado com sucesso.');
      } else {
        await fornecedorService.criar(payload);
        sucesso('Fornecedor salvo com sucesso.');
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
      titulo={fornecedor ? `Editar fornecedor #${fornecedor.id}` : 'Novo fornecedor'}
      subtitulo="Dados do parceiro usado nas compras de mercadoria."
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
          <span>CNPJ *</span>
          <input value={campos.cnpj} onChange={(e) => alterar('cnpj', formatCpfCnpj(e.target.value))} placeholder="00.000.000/0000-00" />
        </label>

        <label className="field">
          <span>Inscrição estadual *</span>
          <input
            value={campos.inscricaoEstadual}
            onChange={(e) => alterar('inscricaoEstadual', onlyDigits(e.target.value).slice(0, 14))}
            placeholder="14 dígitos"
          />
        </label>

        <label className="field">
          <span>Telefone *</span>
          <input value={campos.telefone} onChange={(e) => alterar('telefone', formatPhone(e.target.value))} />
        </label>

        <label className="field">
          <span>E-mail *</span>
          <input type="email" value={campos.email} onChange={(e) => alterar('email', e.target.value)} />
        </label>

        <div className="form-actions span-2">
          <button type="button" className="btn btn-ghost" onClick={onFechar} disabled={salvando}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar fornecedor'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
