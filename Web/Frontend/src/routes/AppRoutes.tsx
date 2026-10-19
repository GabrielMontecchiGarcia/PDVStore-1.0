import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from '../components/Layout';
import { Protegida, RedirecionarInicio } from './ProtectedRoute';
import { LoginPage } from '../pages/LoginPage';
import { DashboardPage } from '../pages/DashboardPage';
import { PdvPage } from '../pages/PdvPage';
import { ProdutosPage } from '../pages/ProdutosPage';
import { EstoquePage } from '../pages/EstoquePage';
import { ClientesPage } from '../pages/ClientesPage';
import { FornecedoresPage } from '../pages/FornecedoresPage';
import { ComprasPage } from '../pages/ComprasPage';
import { CaixaPage } from '../pages/CaixaPage';
import { UsuariosPage } from '../pages/UsuariosPage';
import { SobrePage } from '../pages/SobrePage';
import { NotFoundPage } from '../pages/NotFoundPage';

// ============================================================================
// Mapa de rotas da aplicação (matriz de acesso igual ao menu WinForms).
// ============================================================================

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<Protegida />}>
        <Route element={<Layout />}>
          <Route index element={<RedirecionarInicio />} />

          <Route
            path="dashboard"
            element={<Protegida perfis={['Administrador']}><DashboardPage /></Protegida>}
          />
          <Route
            path="pdv"
            element={<Protegida perfis={['Administrador', 'Operador']}><PdvPage /></Protegida>}
          />
          <Route
            path="produtos"
            element={<Protegida perfis={['Administrador', 'Estoquista']}><ProdutosPage /></Protegida>}
          />
          <Route
            path="estoque"
            element={<Protegida perfis={['Administrador']}><EstoquePage /></Protegida>}
          />
          <Route
            path="clientes"
            element={<Protegida perfis={['Administrador']}><ClientesPage /></Protegida>}
          />
          <Route
            path="fornecedores"
            element={<Protegida perfis={['Administrador']}><FornecedoresPage /></Protegida>}
          />
          <Route
            path="compras"
            element={<Protegida perfis={['Administrador']}><ComprasPage /></Protegida>}
          />
          <Route
            path="caixa"
            element={<Protegida perfis={['Administrador']}><CaixaPage /></Protegida>}
          />
          <Route
            path="usuarios"
            element={<Protegida perfis={['Administrador']}><UsuariosPage /></Protegida>}
          />
          <Route path="sobre" element={<SobrePage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
