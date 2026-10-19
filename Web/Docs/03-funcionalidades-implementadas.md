# Funcionalidades Implementadas

Registro por funcionalidade: objetivo, arquivos principais, regras preservadas,
endpoints/telas e impactos na arquitetura. Todo o código está comentado em
português (objetivo, regra de negócio, integrações e decisões).

---

## 1. Autenticação, sessão e perfis

**Objetivo:** permitir login por usuário/senha, bloquear inativos, emitir JWT e
restringir telas/ações por perfil, como no `frmLogin`/`frmMenuPrincipal`.

**Arquivos**
- `Backend/API/Controllers/AuthController.cs`
- `Backend/API/Security/JwtOptions.cs`, `JwtTokenService.cs`
- `Backend/API/Common/ClaimsExtensions.cs`
- `Backend/Application/Validators/UsuarioValidators.cs`
- `Backend/Services/UsuarioService.cs`
- `Frontend/src/contexts/AuthContext.tsx`, `services/authService.ts`, `pages/LoginPage.tsx`,
  `routes/ProtectedRoute.tsx`

**Regras preservadas**
- Login pelo nome; senha conferida com BCrypt (`UsuarioCaixa.Autenticar`).
- Credenciais inválidas → “Credenciais inválidas!” (HTTP 401).
- Usuário inativo → “Usuário inativo. Contate o administrador.” (HTTP 403).
- Perfis: Administrador (tudo), Operador (PDV), Estoquista (Produtos).
- Token com Id, nome e perfil; operador de venda sempre vem do token.
- Rate limit de 10 tentativas/min por IP no login.

**Endpoints/telas:** `POST /api/auth/login`, `GET /api/auth/me`,
`POST /api/auth/alterar-senha`; tela de login, menu lateral por perfil, modal de
troca de senha.

**Impactos:** substitui `Session.CurrentUser` por claims JWT; cada requisição tem
um `DbContext` scoped em vez de um escopo por login.

---

## 2. PDV / Vendas

**Objetivo:** registrar vendas com caixa aberto, carrinho, desconto, cinco formas
de pagamento, troco, fiado e recibo.

**Arquivos**
- `Backend/Services/VendaService.cs`, `VendaService.Registro.cs`, `.Regras.cs`, `.Consultas.cs`
- `Backend/Infrastructure/Integrations/PagamentoIntegrator.cs`
- `Backend/API/Controllers/VendasController.cs`
- `Backend/Application/Validators/OperacaoValidators.cs`
- `Frontend/src/pages/PdvPage.tsx`, `hooks/useCarrinho.ts`,
  `components/PdvProductList.tsx`, `PdvCart.tsx`, `PdvCheckout.tsx`, `ReciboModal.tsx`

**Regras preservadas**
- Caixa aberto obrigatório; ≥ 1 item; forma de pagamento válida.
- `ValorTotal = Σ(quantidade × preço) − desconto`, nunca negativo.
- Estoque validado por produto antes de gravar; baixa + movimentação “Saída”.
- Fiado: cliente ativo obrigatório; bloqueio quando `saldo + total > limite` e
  `limite > 0`; soma ao saldo devedor.
- Dinheiro: valor recebido deve cobrir o total (troco ≥ 0).
- PIX: gera `PixTxId` no mock; pagamento sempre aprovado; falha desfaz a transação.
- Recibo com itens, desconto, total, forma, troco, PIX e assinatura (fiado), com
  impressão pelo navegador.

**Endpoints/telas:** `POST /api/vendas`, `GET /api/vendas/{id}`, `GET /api/vendas`,
`POST /api/vendas/{id}/cancelar`; tela PDV.

**Impactos:** transação explícita via Unit of Work; regra crítica centralizada no
serviço e replicada (não substituída) no cliente para feedback imediato.

---

## 3. Cancelamento de venda

**Objetivo:** desfazer uma venda concluída com estorno de estoque e fiado.

**Arquivos:** `Backend/Services/VendaService.Consultas.cs`,
`API/Controllers/VendasController.cs`.

**Regras:** retorna falha se a venda não existe ou já está cancelada; devolve cada
item ao estoque; grava movimentação “Entrada” com motivo `Estorno - Venda #id`;
reduz `SaldoDevedor` com `Math.Max(0, ...)` para vendas fiadas; marca “Cancelada”.
O motivo é registrado em log (não existe coluna no schema).

**Endpoint:** `POST /api/vendas/{id}/cancelar` (Administrador).

---

## 4. Caixa

**Objetivo:** abrir/fechar turno, registrar sangria e consultar histórico.

**Arquivos:** `Backend/Services/CaixaService.cs`,
`API/Controllers/CaixasController.cs`, `Frontend/src/pages/CaixaPage.tsx`,
`services/caixaService.ts`.

**Regras:** apenas um caixa aberto; abertura grava valor inicial, usuário e UTC;
`ValorFinal = inicial + vendas concluídas do caixa − sangrias`; sangria > 0 e só em
caixa aberto; listagem da mais recente para a mais antiga com usuário.

**Endpoints/telas:** `GET /api/caixas/aberto`, `GET /api/caixas`,
`POST /api/caixas/abrir`, `/{id}/sangria`, `/{id}/fechar`; tela Caixa.

---

## 5. Produtos e Estoque

**Objetivo:** cadastrar produtos e controlar saldo/movimentações.

**Arquivos:** `Backend/Services/ProdutoService.cs`, `API/Controllers/ProdutosController.cs`,
`Application/Validators/CadastroValidators.cs`, `Frontend/src/pages/ProdutosPage.tsx`,
`EstoquePage.tsx`, `components/ProdutoFormModal.tsx`, `services/produtoService.ts`.

**Regras:** código de barras, nome, preço, custo, estoque, mínimo, categoria e
descrição obrigatórios; valores ≥ 0; soft delete; busca por nome/código apenas
ativos; entrada/saída manual > 0 com movimentação; saída bloqueada sem saldo.

**Endpoints/telas:** CRUD `/api/produtos`, busca por código de barras,
`/api/produtos/movimentacoes`, `/estoque/entrada` e `/estoque/saida`; telas
Produtos e Estoque. Exportação Excel/PDF do cadastro.

---

## 6. Clientes e fiado (caderneta)

**Objetivo:** manter clientes, limite de crédito, saldo devedor e baixa de fiado.

**Arquivos:** `Backend/Services/ClienteService.cs`,
`API/Controllers/ClientesFornecedoresController.cs`, `Frontend/src/pages/ClientesPage.tsx`,
`components/ClienteFormModal.tsx`, `ReceberFiadoModal.tsx`.

**Regras:** nome obrigatório; CPF/CNPJ válido (11/14); telefone ≥ 10 dígitos;
e-mail válido; endereço obrigatório; limite ≥ 10; soft delete; recebimento > 0 com
`Math.Max(0, ...)`; valor maior que o débito registra apenas o valor devido (com
confirmação), como na tela original.

**Endpoints/telas:** CRUD `/api/clientes`, `PUT /{id}/status`,
`POST /{id}/receber-fiado`; tela Clientes. Exportação Excel/PDF.

---

## 7. Fornecedores

**Objetivo:** cadastrar parceiros usados nas compras.

**Arquivos:** `Backend/Services/FornecedorService.cs`,
`API/Controllers/ClientesFornecedoresController.cs`, `Frontend/src/pages/FornecedoresPage.tsx`,
`components/FornecedorFormModal.tsx`.

**Regras:** nome, CNPJ válido (14), inscrição estadual válida (14), telefone ≥ 10 e
email válido; soft delete preservando compras históricas; filtro por nome/CNPJ.

**Endpoints/telas:** CRUD `/api/fornecedores`; tela Fornecedores. Exportação.

---

## 8. Compras

**Objetivo:** registrar notas de fornecedores com entrada automática no estoque.

**Arquivos:** `Backend/Services/CompraService.cs`, `.Consultas.cs`,
`API/Controllers/ComprasController.cs`, `Frontend/src/pages/ComprasPage.tsx`,
`services/compraService.ts`.

**Regras:** fornecedor e ≥ 1 item; quantidade > 0; custo ≥ 0; produtos existentes;
`ValorTotal = Σ subtotais`; transação grava compra, soma estoque, atualiza custo
quando > 0 e registra movimentação “Entrada” com nota; cancelamento reduz estoque
(`Math.Max(0, ...)`) e marca “Cancelada” (sem movimentação de estorno, como no
original).

**Endpoints/telas:** `POST/GET /api/compras`, `POST /{id}/cancelar`; tela Compras.
Exportação.

---

## 9. Dashboard e relatórios

**Objetivo:** consolidar indicadores e exportar PDF/Excel.

**Arquivos:** `Backend/Services/RelatorioService.cs`, `ExportacaoService.cs`,
`API/Controllers/DashboardController.cs`, `RelatoriosController*.cs`,
`Frontend/src/pages/DashboardPage.tsx`, `components/Charts.tsx`, `DashboardTables.tsx`,
`ExportButtons.tsx`, `services/relatorioService.ts`.

**Regras/indicadores:** total e quantidade de vendas concluídas; pagamentos por
forma; top 10 itens vendidos; produtos ativos com `Estoque <= EstoqueMinimo`;
movimentações por dia (entradas × saídas); vendas por dia. Período padrão: 1º dia
do mês até hoje. Datas agrupadas no fuso `App:TimeZone`.

**Endpoints/telas:** `GET /api/dashboard`, `/api/relatorios/itens-mais-vendidos`,
`/estoque-minimo`, `/total-vendas`, `/exportar/{tipo}/{excel|pdf}`; tela Dashboard.

**Impactos:** exportações server-side (ClosedXML/QuestPDF) substituem iText/EPPlus,
com Reflexão sobre os DTOs para montar colunas, mantendo o objetivo das telas.

---

## 10. Usuários

**Objetivo:** cadastrar/editar/desativar usuários e atribuir perfis.

**Arquivos:** `Backend/Services/UsuarioService.cs`, `API/Controllers/UsuariosController.cs`,
`Application/Validators/UsuarioValidators.cs`, `Frontend/src/pages/UsuariosPage.tsx`,
`components/UsuarioFormModal.tsx`, `services/usuarioService.ts`.

**Regras:** nome obrigatório e único; senha ≥ 6 e confirmação igual; edição mantém
o nome imutável e redefine a senha (como a tela original); soft delete; perfis
válidos (1/2/3). Hash BCrypt nunca retornado. Exportação Excel/PDF.

**Endpoints/telas:** CRUD `/api/usuarios`; tela Usuários.

---

## 11. Transversais

| Item | Arquivos | Descrição |
|---|---|---|
| Exceções globais | `Middleware/GlobalExceptionMiddleware.cs`, Program | HTTP 400/403/404/409/500 com `ErroResposta` |
| Logs | Program (Serilog) | Console + arquivo diário, 14 dias |
| Segurança | Program, JWT, policies, rate limit | JWT, perfis, headers, CORS, força bruta |
| Validações | `Application/Validators`, `utils/masks.ts` | Server-side e client-side |
| Máscaras | `Domain/Common/Mascaras*.cs`, `utils/masks.ts` | CPF, CNPJ, IE, telefone, decimal, e-mail |
| DTO/Mapeamento | `DTOs/`, `Application/Mappings` | Contratos e projeções |
| Banco | `Repository/`, `Database/` | EF Core, migrations, scripts e procedures |
| Testes | `Tests/` (72) | Regras, validadores e máscaras |

## 12. Foto do usuário (upload)

**Objetivo:** reproduzir o botão "Escolher Foto" do `frmCadastroUsuario`, que
copia a imagem para a pasta `Resources/Users` do sistema Windows.

**Arquivos**
- `Backend/API/Controllers/UsuariosFotoController.cs`
- `Backend/API/wwwroot/uploads/usuarios/` (pasta servida estaticamente)
- `Frontend/src/components/UsuarioFormModal.tsx` (seleção + preview)
- `Frontend/src/components/Layout.tsx` (avatar com a foto no topo)
- `Frontend/src/services/usuarioService.ts`, `utils/download.ts` (`urlDeArquivo`)

**Regras/segurança:** exclusivo do Administrador; até 2 MB; extensões
jpg/jpeg/png/bmp; validação dos bytes mágicos; nome de arquivo gerado pelo
servidor; gravação em `wwwroot/uploads/usuarios`. Sem nova foto, a anterior é
preservada (como na tela original).

**Endpoint:** `POST /api/usuarios/foto` (multipart) → `{ "caminho": "/uploads/usuarios/..." }`.

---

## 13. Tela de Vendas e cancelamento

**Objetivo:** expor na web a regra de cancelamento que já existia no
`VendaService.CancelarVendaAsync` (devolve estoque, registra estorno e reduz o
fiado), permitindo ao Administrador a consultar o histórico de vendas do período.

**Arquivos**
- `Backend/API/Controllers/VendasController.cs` (consulta + cancelamento)
- `Frontend/src/pages/VendasPage.tsx`, `routes/AppRoutes.tsx`, `components/Layout.tsx`

**Regras:** listagem por período (padrão: mês atual); cancelamento somente de venda
`Concluida`; motivo registrado em log (não existe coluna); Indicadores de
faturamento, quantidade, ticket médio e vendas canceladas; exportação Excel/PDF.

**Endpoints/telas:** `GET /api/vendas`, `POST /api/vendas/{id}/cancelar`; tela
**Vendas** (Administrador).

---

## 14. Testes de integração da API

**Objetivo:** validar a API de ponta a ponta (HTTP, autenticação, autorização,
validações e regras) sem depender de SQL Server.

**Arquivos:** `Backend/Tests/Integration/ApiFactory.cs`, `ApiIntegrationTests.cs`,
`ApiIntegrationTests.Vendas.cs`, `Backend/Tests/AssemblyInfo.cs`.

**Cenários:** login válido (200 + token + perfil), senha incorreta (401 com
mensagem em PT-BR), rota protegida sem token (401), Operador bloqueado em
dashboard/lista de vendas (403), venda sem caixa (400 com a mensagem do PDV),
venda com PIX concluída (201 + TxId + baixa de estoque), cliente inválido (400
com detalhes em PT-BR), exportação Excel (200 + content-type) e upload de foto
(extensão inválida 400, PNG válido 200).

**Total da suíte:** 82 testes (74 unitários + 8 de integração).

---

## 15. Continuous Integration e ambiente

| Item | Arquivo | Descrição |
|---|---|---|
| CI web | `.github/workflows/ci-web.yml` | Build/testes .NET 8 (ubuntu) + typecheck/build React; o workflow do projeto Windows segue intacto |
| SQL Server local | `Web/docker-compose.yml` | Container `mssql/server:2022-latest` (porta 1433) com volume persistente |
| Dados de exemplo (SQL Server) | `Database/PDVStore/Scripts/005_dados_exemplo.sql` | Produtos, clientes e fornecedor de demonstração (idempotente, opcional) |

---

## 16. Rastreabilidade das regras

As mensagens e decisões foram mantidas iguais às do sistema Windows. Onde a UI e o
serviço original divergiam, a escolha está documentada na seção 7 de
`01-analise-sistema-referencia.md` e na seção 10 de
`02-arquitetura-e-decisoes.md`, sempre com justificativa e sem alterar o projeto
original.
