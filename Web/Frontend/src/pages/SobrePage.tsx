import { useDocumentTitle } from '../hooks/useDebounce';
import { PageHeader } from '../components/PageHeader';
import { Icon } from '../components/Icon';

// ============================================================================
// Sobre o sistema (equivalente ao frmSobre), com informações do projeto web e
// da matriz de permissões herdada do sistema Windows.
// ============================================================================

const FUNCIONALIDADES = [
  'Vendas no PDV com exigência de caixa aberto',
  'Formas de pagamento: Dinheiro, PIX, Cartão de Crédito, Cartão de Débito e Fiado',
  'Venda fiada com limite de crédito e saldo devedor',
  'Cadastro de produtos com preço de custo e estoque mínimo',
  'Gestão de estoque com entrada/saída manual e histórico',
  'Fornecedores e compras com entrada automática no estoque',
  'Abertura e fechamento de caixa com sangria',
  'Dashboard e relatórios com exportação PDF e Excel',
  'Usuários e perfis: Administrador, Operador (Caixa) e Estoquista'
];

export function SobrePage() {
  useDocumentTitle('Sobre');

  return (
    <div className="page">
      <PageHeader titulo="Sobre o PDVStore Web" descricao="Sistema de Ponto de Venda para pequenas lojas e comércios de bairro." />

      <section className="card sobre-hero">
        <span className="brand-mark brand-mark-lg">PDV</span>
        <div>
          <h2>PDVStore Web 1.0</h2>
          <p>
            Versão web derivada do sistema PDVStore (Windows Forms), mantendo a mesma lógica de
            negócio, nomenclatura e regras de validação. O projeto original permanece intacto.
          </p>
        </div>
      </section>

      <div className="grid-2">
        <section className="card">
          <header className="card-header"><div><h2>Funcionalidades</h2><p>Recursos disponíveis nesta versão.</p></div></header>
          <div className="card-body">
            <ul className="lista-check">
              {FUNCIONALIDADES.map((item) => (
                <li key={item}><Icon name="check" size={16} /> {item}</li>
              ))}
            </ul>
          </div>
        </section>

        <section className="card">
          <header className="card-header"><div><h2>Perfis de acesso</h2><p>Mesma matriz do menu original.</p></div></header>
          <div className="card-body no-padding">
            <table className="table">
              <thead><tr><th>Perfil</th><th>Acesso</th></tr></thead>
              <tbody>
                <tr><td><strong>Administrador</strong></td><td>Todas as funcionalidades</td></tr>
                <tr><td><strong>Operador (Caixa)</strong></td><td>Somente o PDV</td></tr>
                <tr><td><strong>Estoquista</strong></td><td>Somente o cadastro de produtos</td></tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <div className="grid-2">
        <section className="card">
          <header className="card-header"><div><h2>Tecnologias</h2><p>Arquitetura da versão web.</p></div></header>
          <div className="card-body">
            <ul className="lista-tecnologias">
              <li><Icon name="layers" size={16} /> <strong>Frontend:</strong> React + TypeScript + Vite</li>
              <li><Icon name="box" size={16} /> <strong>Backend:</strong> C# / .NET 8 (Clean Architecture, Repository, DTO, DI)</li>
              <li><Icon name="cash" size={16} /> <strong>Banco:</strong> SQL Server + Entity Framework Core</li>
              <li><Icon name="lock" size={16} /> <strong>Segurança:</strong> JWT, BCrypt, autorização por perfil e validação server-side</li>
              <li><Icon name="download" size={16} /> <strong>Relatórios:</strong> exportação Excel (ClosedXML) e PDF (QuestPDF)</li>
            </ul>
          </div>
        </section>

        <section className="card">
          <header className="card-header"><div><h2>Informações do projeto</h2><p>Licença e observações.</p></div></header>
          <div className="card-body">
            <dl className="def-list">
              <dt>Licença</dt><dd>MIT</dd>
              <dt>Origem</dt><dd>PDVStore 1.0 (Windows Forms)</dd>
              <dt>Instituição</dt><dd>Curso Técnico em Informática — SENAC Ji-Paraná</dd>
              <dt>Integração de pagamento</dt><dd>Mock didático para PIX/cartão (sem adquirente real)</dd>
              <dt>Logs</dt><dd>Arquivo diário em logs/pdvstore-web-AAAAMMDD.log</dd>
            </dl>
          </div>
        </section>
      </div>
    </div>
  );
}
