import axios, { AxiosError } from 'axios';

// ============================================================================
// Cliente HTTP central da aplicação.
// - Base URL configurável por VITE_API_URL (padrão: proxy /api do Vite).
// - Injeta o token JWT salvo na sessão em todas as requisições.
// - Normaliza erros da API (ErroResposta/ProblemDetails) em ApiError com
//   mensagens em português, exibidas pelos componentes.
// ============================================================================

export const TOKEN_KEY = 'pdvstore.token';
export const USUARIO_KEY = 'pdvstore.usuario';

export class ApiError extends Error {
  status: number;
  details: string[];

  constructor(mensagem: string, status = 0, details: string[] = []) {
    super(mensagem);
    this.name = 'ApiError';
    this.status = status;
    this.details = details;
  }
}

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL?.trim() || '/api',
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' }
});

api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

interface ErroBackend {
  mensagem?: string;
  title?: string;
  detail?: string;
  detalhes?: string[];
  errors?: Record<string, string[]>;
}

const extrairMensagem = (dados: ErroBackend | undefined): { mensagem: string; detalhes: string[] } => {
  const detalhes: string[] = [];

  if (dados?.detalhes?.length) detalhes.push(...dados.detalhes);

  if (dados?.errors) {
    for (const mensagens of Object.values(dados.errors)) {
      if (Array.isArray(mensagens)) detalhes.push(...mensagens);
    }
  }

  const mensagem =
    dados?.mensagem ||
    (detalhes.length > 0 ? detalhes[0] : undefined) ||
    dados?.title ||
    dados?.detail ||
    'Não foi possível concluir a operação.';

  return { mensagem, detalhes: Array.from(new Set(detalhes)) };
};

api.interceptors.response.use(
  (resposta) => resposta,
  (erro: AxiosError<ErroBackend>) => {
    const status = erro.response?.status ?? 0;
    const dados = erro.response?.data;
    const { mensagem, detalhes } = extrairMensagem(dados);

    // Token expirado/inválido: dispara logout global (AuthContext escuta).
    const url = erro.config?.url ?? '';
    if (status === 401 && !url.includes('/auth/login')) {
      window.dispatchEvent(new Event('pdv:unauthorized'));
    }

    if (status === 0) {
      return Promise.reject(new ApiError(
        'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.',
        0
      ));
    }

    return Promise.reject(new ApiError(mensagem, status, detalhes));
  }
);
