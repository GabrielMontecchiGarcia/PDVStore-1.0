import { useMemo, useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import type { PermissaoNome } from '../models';
import { Icon, type IconName } from './Icon';
import { AlterarSenhaModal } from './AlterarSenhaModal';
import { urlDeArquivo } from '../utils/download';

// ============================================================================
// Layout autenticado: menu lateral com itens filtrados por perfil (mesma matriz
// do frmMenuPrincipal), barra superior com usuário e ações de sessão.
// ============================================================================

interface ItemMenu {
  para: string;
  rotulo: string;
  icone: IconName;
  perfis: PermissaoNome[];
}

const ITENS_MENU: ItemMenu[] = [
  { para: '/dashboard', rotulo: 'Dashboard', icone: 'dashboard', perfis: ['Administrador'] },
  { para: '/pdv', rotulo: 'Vender (PDV)', icone: 'cart', perfis: ['Administrador', 'Operador'] },
  { para: '/produtos', rotulo: 'Produtos', icone: 'box', perfis: ['Administrador', 'Estoquista'] },
  { para: '/estoque', rotulo: 'Estoque', icone: 'layers', perfis: ['Administrador'] },
  { para: '/clientes', rotulo: 'Clientes', icone: 'users', perfis: ['Administrador'] },
  { para: '/fornecedores', rotulo: 'Fornecedores', icone: 'truck', perfis: ['Administrador'] },
  { para: '/compras', rotulo: 'Compras', icone: 'shopping', perfis: ['Administrador'] },
  { para: '/vendas', rotulo: 'Vendas', icone: 'receipt', perfis: ['Administrador'] },
  { para: '/caixa', rotulo: 'Abrir/Fechar Caixa', icone: 'cash', perfis: ['Administrador'] },
  { para: '/usuarios', rotulo: 'Usuários', icone: 'userCog', perfis: ['Administrador'] },
  { para: '/sobre', rotulo: 'Sobre', icone: 'info', perfis: ['Administrador', 'Operador', 'Estoquista'] }
];

export function Layout() {
  const { usuario, sair } = useAuth();
  const navigate = useNavigate();
  const [menuAberto, setMenuAberto] = useState(false);
  const [modalSenha, setModalSenha] = useState(false);

  const itens = useMemo(
    () => ITENS_MENU.filter((item) => usuario && item.perfis.includes(usuario.permissao)),
    [usuario]
  );

  const iniciais = (usuario?.nome ?? '?')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte[0]?.toUpperCase())
    .join('');

  // Foto cadastrada do usuário (enviada pela API) — fallback: iniciais.
  const fotoUsuario = urlDeArquivo(usuario?.fotoPath);

  const encerrarSessao = () => {
    sair();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app-shell">
      {menuAberto && <button className="sidebar-backdrop" aria-label="Fechar menu" onClick={() => setMenuAberto(false)} />}

      <aside className={`sidebar ${menuAberto ? 'sidebar-open' : ''}`}>
        <div className="brand">
          <span className="brand-mark">PDV</span>
          <div>
            <strong>PDVStore</strong>
            <small>Web · Ponto de Venda</small>
          </div>
        </div>

        <nav className="sidebar-nav">
          {itens.map((item) => (
            <NavLink
              key={item.para}
              to={item.para}
              className={({ isActive }) => `nav-item ${isActive ? 'nav-item-active' : ''}`}
              onClick={() => setMenuAberto(false)}
            >
              <Icon name={item.icone} size={19} />
              <span>{item.rotulo}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar-footer">
          <span>Versão Web 1.0</span>
          <span>© 2026 PDVStore</span>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <button className="icon-button topbar-menu" onClick={() => setMenuAberto((v) => !v)} aria-label="Abrir menu">
            <Icon name="menu" />
          </button>

          <div className="topbar-context">
            <span className="topbar-date">
              {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' })}
            </span>
          </div>

          <div className="topbar-user">
            {fotoUsuario ? (
              <span className="avatar avatar-foto">
                <img src={fotoUsuario} alt={`Foto de ${usuario?.nome ?? 'usuário'}`} />
              </span>
            ) : (
              <div className="avatar" aria-hidden="true">{iniciais}</div>
            )}
            <div className="user-info">
              <strong>{usuario?.nome}</strong>
              <small>{usuario?.permissaoDescricao}</small>
            </div>

            <button type="button" className="icon-button" title="Alterar senha" onClick={() => setModalSenha(true)}>
              <Icon name="lock" size={18} />
            </button>
            <button type="button" className="icon-button danger" title="Sair" onClick={encerrarSessao}>
              <Icon name="logout" size={18} />
            </button>
          </div>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>

      <AlterarSenhaModal aberto={modalSenha} onFechar={() => setModalSenha(false)} />
    </div>
  );
}
