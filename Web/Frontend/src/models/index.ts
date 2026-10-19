// ============================================================================
// Modelos da aplicação web — espelham os DTOs da API e as entidades do sistema
// Windows de referência. Nomes de campos iguais aos do backend (camelCase JSON).
// ============================================================================

export type PermissaoNome = 'Administrador' | 'Operador' | 'Estoquista';

export interface Usuario {
  id: number;
  nome: string;
  permissao: PermissaoNome;
  permissaoCodigo: number;
  permissaoDescricao: string;
  fotoPath?: string | null;
  ativo: boolean;
  createdAt: string;
}

export interface LoginResponse {
  token: string;
  expiraEm: string;
  usuario: Usuario;
}

export interface Produto {
  id: number;
  nome: string;
  codigoBarras?: string | null;
  preco: number;
  precoCusto: number;
  estoque: number;
  estoqueAtual: number;
  estoqueMinimo: number;
  categoria?: string | null;
  descricao?: string | null;
  ativo: boolean;
}

export interface ProdutoRequest {
  codigoBarras: string;
  nome: string;
  preco: number;
  precoCusto: number;
  estoque: number;
  estoqueMinimo: number;
  categoria: string;
  descricao: string;
}

export interface Cliente {
  id: number;
  nome: string;
  cpfCnpj?: string | null;
  telefone?: string | null;
  email?: string | null;
  endereco?: string | null;
  cadastradoEm: string;
  ativo: boolean;
  limiteCredito: number;
  saldoDevedor: number;
}

export interface ClienteRequest {
  nome: string;
  cpfCnpj: string;
  telefone: string;
  email: string;
  endereco: string;
  limiteCredito: number;
}

export interface Fornecedor {
  id: number;
  nome: string;
  cnpj?: string | null;
  inscricaoEstadual?: string | null;
  telefone?: string | null;
  email?: string | null;
  ativo: boolean;
}

export interface FornecedorRequest {
  nome: string;
  cnpj: string;
  inscricaoEstadual: string;
  telefone: string;
  email: string;
}

export interface Caixa {
  id: number;
  usuarioCaixaId: number;
  usuarioNome?: string | null;
  abertura: string;
  fechamento?: string | null;
  valorInicial: number;
  valorFinal?: number | null;
  sangria?: number | null;
  status: string;
}

export interface MovimentacaoEstoque {
  id: number;
  produtoId: number;
  produtoNome?: string | null;
  tipo: 'Entrada' | 'Saída';
  quantidade: number;
  precoUnitario?: number | null;
  dataMovimentacao: string;
  usuarioId?: number | null;
  usuarioNome?: string | null;
  motivo?: string | null;
  referenciaVendaId?: number | null;
}

export interface ItemVendaRequest {
  produtoId: number;
  quantidade: number;
  precoUnitario: number;
}

export interface VendaRequest {
  itens: ItemVendaRequest[];
  desconto: number;
  formaPagamento: string;
  clienteId?: number | null;
  valorRecebido?: number | null;
}

export interface ItemVenda {
  id: number;
  produtoId: number;
  produtoNome?: string | null;
  quantidade: number;
  precoUnitario: number;
  subtotal: number;
}

export interface Venda {
  id: number;
  usuarioCaixaId: number;
  usuarioNome?: string | null;
  caixaId: number;
  clienteId?: number | null;
  clienteNome?: string | null;
  dataVenda: string;
  valorTotal: number;
  desconto: number;
  formaPagamento: string;
  pixTxId?: string | null;
  status: string;
  itens: ItemVenda[];
}

export interface ItemCompraRequest {
  produtoId: number;
  quantidade: number;
  precoCusto: number;
}

export interface CompraRequest {
  fornecedorId: number;
  numeroNota?: string | null;
  itens: ItemCompraRequest[];
}

export interface ItemCompra {
  id: number;
  produtoId: number;
  produtoNome?: string | null;
  quantidade: number;
  precoCusto: number;
  subtotal: number;
}

export interface Compra {
  id: number;
  fornecedorId: number;
  fornecedorNome?: string | null;
  usuarioCaixaId: number;
  usuarioNome?: string | null;
  dataCompra: string;
  numeroNota?: string | null;
  valorTotal: number;
  status: string;
  itens: ItemCompra[];
}

export interface ItemRelatorio {
  nomeProduto: string;
  codigoBarras?: string | null;
  totalVendido: number;
  valorTotalVendido: number;
  estoqueAtual: number;
  estoqueMinimo: number;
  statusMinimo: string;
}

export interface PagamentoPorForma {
  forma: string;
  quantidade: number;
  total: number;
}

export interface MovimentacaoPorDia {
  dia: string;
  entradas: number;
  saidas: number;
}

export interface VendaPorDia {
  dia: string;
  totalVendas: number;
}

export interface Dashboard {
  inicio: string;
  fim: string;
  totalPeriodo: number;
  quantidadeVendas: number;
  pagamentosPorForma: PagamentoPorForma[];
  itensMaisVendidos: ItemRelatorio[];
  alertasEstoque: Produto[];
  movimentacoesPorDia: MovimentacaoPorDia[];
  vendasPorDia: VendaPorDia[];
}

export interface ErroApi {
  status: number;
  mensagem: string;
  detalhes?: string[] | null;
  traceId?: string | null;
  timestamp?: string;
}

// Formas de pagamento — mesmas strings gravadas pela aplicação Windows.
export const FORMAS_PAGAMENTO = [
  'Dinheiro',
  'PIX',
  'Cartão Crédito',
  'Cartão Débito',
  'Fiado'
] as const;

export type FormaPagamento = (typeof FORMAS_PAGAMENTO)[number];

export const PERMISSOES = [
  { codigo: 1, nome: 'Operador', descricao: 'Operador (Caixa) - apenas vendas (PDV)' },
  { codigo: 3, nome: 'Estoquista', descricao: 'Estoquista - apenas cadastro de produtos' },
  { codigo: 2, nome: 'Administrador', descricao: 'Administrador - acesso total' }
] as const;
