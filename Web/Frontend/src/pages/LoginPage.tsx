import { useEffect, useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { inicioDoPerfil } from '../routes/ProtectedRoute';
import { mensagemDeErro } from '../utils/format';
import { Icon } from '../components/Icon';

// ============================================================================
// Tela de login. Reproduz o fluxo do frmLogin: usuário/senha obrigatórios,
// mensagens específicas para credenciais inválidas e usuário inativo.
// ============================================================================

export function LoginPage() {
  const { usuario, carregando, entrar } = useAuth();
  const navigate = useNavigate();
  const local = useLocation();

  const [nome, setNome] = useState('');
  const [senha, setSenha] = useState('');
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [erro, setErro] = useState('');

  useEffect(() => {
    document.title = 'Entrar · PDVStore';
  }, []);

  if (carregando) {
    return (
      <div className="tela-carregando">
        <span className="spinner" />
        <span>Carregando sessão...</span>
      </div>
    );
  }

  if (usuario) {
    return <Navigate to={inicioDoPerfil(usuario.permissao)} replace />;
  }

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErro('');

    if (!nome.trim() || !senha.trim()) {
      setErro('Informe usuário e senha.');
      return;
    }

    setEnviando(true);
    try {
      const autenticado = await entrar(nome, senha);
      const destino = (local.state as { de?: string } | null)?.de;
      navigate(destino && destino !== '/login' ? destino : inicioDoPerfil(autenticado.permissao), { replace: true });
    } catch (e) {
      setErro(mensagemDeErro(e));
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="login-page">
      <section className="login-brand">
        <div className="login-logo">
          <span className="brand-mark brand-mark-lg">PDV</span>
          <div>
            <h1>PDVStore</h1>
            <p>Ponto de venda para pequenas lojas e comércios de bairro</p>
          </div>
        </div>

        <ul className="login-features">
          <li><Icon name="cart" size={18} /> Vendas com controle de caixa e cinco formas de pagamento</li>
          <li><Icon name="users" size={18} /> Fiado com limite de crédito e saldo devedor por cliente</li>
          <li><Icon name="box" size={18} /> Estoque, compras e alertas de reposição</li>
          <li><Icon name="dashboard" size={18} /> Dashboard e relatórios com exportação em PDF e Excel</li>
        </ul>

        <div className="login-brand-footer">
          <span>Versão Web 1.0</span>
          <span>Projeto acadêmico · SENAC Ji-Paraná</span>
        </div>
      </section>

      <section className="login-form-area">
        <form className="login-card" onSubmit={enviar}>
          <header>
            <h2>Acessar o sistema</h2>
            <p>Entre com seu usuário e senha para continuar.</p>
          </header>

          {erro && <div className="alert alert-danger" role="alert">{erro}</div>}

          <label className="field">
            <span>Usuário</span>
            <input
              type="text"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              autoComplete="username"
              autoFocus
              placeholder="Nome de usuário"
            />
          </label>

          <label className="field">
            <span>Senha</span>
            <div className="input-with-action">
              <input
                type={mostrarSenha ? 'text' : 'password'}
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoComplete="current-password"
                placeholder="Sua senha"
              />
              <button
                type="button"
                className="input-action"
                onClick={() => setMostrarSenha((v) => !v)}
                aria-label={mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'}
              >
                <Icon name="eye" size={18} />
              </button>
            </div>
          </label>

          <button type="submit" className="btn btn-primary btn-block btn-lg" disabled={enviando}>
            {enviando ? 'Entrando...' : 'Entrar'}
          </button>

          <p className="login-hint">
            Primeiro acesso: <strong>Admin</strong> / <strong>admin123</strong>. Troque a senha após entrar.
          </p>
        </form>
      </section>
    </div>
  );
}
