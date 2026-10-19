import { useEffect, useState, type FormEvent } from 'react';
import type { Produto, ProdutoRequest } from '../models';
import { produtoService } from '../services/produtoService';
import { useToast } from '../contexts/ToastContext';
import { mensagemDeErro } from '../utils/format';
import { parseDecimal, toDecimalInput } from '../utils/masks';
import { Modal } from './Modal';

// ============================================================================
// Formulário de produto (criar/editar) com as mesmas validações da tela
// frmGerenciarProdutos: código, nome, preço, custo, estoque, mínimo, categoria
// e descrição obrigatórios; valores negativos bloqueados.
// ============================================================================

interface ProdutoFormModalProps {
  aberto: boolean;
  produto: Produto | null;
  onFechar: () => void;
  onSalvo: () => void;
}

interface CamposProduto {
  codigoBarras: string;
  nome: string;
  preco: string;
  precoCusto: string;
  estoque: string;
  estoqueMinimo: string;
  categoria: string;
  descricao: string;
}

const vazio: CamposProduto = {
  codigoBarras: '', nome: '', preco: '0,00', precoCusto: '0,00',
  estoque: '0', estoqueMinimo: '0', categoria: '', descricao: ''
};

export function ProdutoFormModal({ aberto, produto, onFechar, onSalvo }: ProdutoFormModalProps) {
  const { sucesso, erro } = useToast();
  const [campos, setCampos] = useState<CamposProduto>(vazio);
  const [erroLocal, setErroLocal] = useState('');
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (!aberto) return;

    setErroLocal('');
    setCampos(produto ? {
      codigoBarras: produto.codigoBarras ?? '',
      nome: produto.nome,
      preco: toDecimalInput(produto.preco),
      precoCusto: toDecimalInput(produto.precoCusto),
      estoque: String(produto.estoque),
      estoqueMinimo: String(produto.estoqueMinimo),
      categoria: produto.categoria ?? '',
      descricao: produto.descricao ?? ''
    } : vazio);
  }, [aberto, produto]);

  const alterar = (campo: keyof CamposProduto, valor: string) =>
    setCampos((atuais) => ({ ...atuais, [campo]: valor }));

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErroLocal('');

    if (!campos.codigoBarras.trim()) { setErroLocal('Código de barras é obrigatório.'); return; }
    if (!campos.nome.trim()) { setErroLocal('Nome do produto é obrigatório.'); return; }

    const preco = parseDecimal(campos.preco);
    const precoCusto = parseDecimal(campos.precoCusto);
    const estoque = Number.parseInt(campos.estoque, 10);
    const estoqueMinimo = Number.parseInt(campos.estoqueMinimo, 10);

    if (!Number.isFinite(estoque) || estoque < 0) { setErroLocal('Informe um estoque válido.'); return; }
    if (!Number.isFinite(estoqueMinimo) || estoqueMinimo < 0) { setErroLocal('Informe um estoque mínimo válido.'); return; }
    if (!campos.categoria.trim()) { setErroLocal('Categoria é obrigatória.'); return; }
    if (!campos.descricao.trim()) { setErroLocal('Descrição é obrigatória.'); return; }

    const payload: ProdutoRequest = {
      codigoBarras: campos.codigoBarras.trim(),
      nome: campos.nome.trim(),
      preco,
      precoCusto,
      estoque,
      estoqueMinimo,
      categoria: campos.categoria.trim(),
      descricao: campos.descricao.trim()
    };

    setSalvando(true);
    try {
      if (produto) {
        await produtoService.atualizar(produto.id, payload);
        sucesso('Produto atualizado com sucesso.');
      } else {
        await produtoService.criar(payload);
        sucesso('Produto salvo com sucesso.');
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
      titulo={produto ? `Editar produto #${produto.id}` : 'Novo produto'}
      subtitulo="Preencha os dados do cadastro. Campos com * são obrigatórios."
      tamanho="lg"
      onFechar={onFechar}
    >
      <form id="form-produto" onSubmit={enviar} className="form-grid">
        {erroLocal && <div className="alert alert-danger span-2">{erroLocal}</div>}

        <label className="field">
          <span>Código de barras *</span>
          <input value={campos.codigoBarras} onChange={(e) => alterar('codigoBarras', e.target.value)} />
        </label>

        <label className="field">
          <span>Nome *</span>
          <input value={campos.nome} onChange={(e) => alterar('nome', e.target.value)} />
        </label>

        <label className="field">
          <span>Preço de venda (R$) *</span>
          <input inputMode="decimal" value={campos.preco} onChange={(e) => alterar('preco', e.target.value)} />
        </label>

        <label className="field">
          <span>Preço de custo (R$) *</span>
          <input inputMode="decimal" value={campos.precoCusto} onChange={(e) => alterar('precoCusto', e.target.value)} />
        </label>

        <label className="field">
          <span>Estoque atual *</span>
          <input type="number" min={0} value={campos.estoque} onChange={(e) => alterar('estoque', e.target.value)} />
        </label>

        <label className="field">
          <span>Estoque mínimo *</span>
          <input type="number" min={0} value={campos.estoqueMinimo} onChange={(e) => alterar('estoqueMinimo', e.target.value)} />
        </label>

        <label className="field">
          <span>Categoria *</span>
          <input value={campos.categoria} onChange={(e) => alterar('categoria', e.target.value)} />
        </label>

        <label className="field span-2">
          <span>Descrição *</span>
          <textarea rows={3} value={campos.descricao} onChange={(e) => alterar('descricao', e.target.value)} />
        </label>

        <div className="form-actions span-2">
          <button type="button" className="btn btn-ghost" onClick={onFechar} disabled={salvando}>Cancelar</button>
          <button type="submit" className="btn btn-primary" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar produto'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
