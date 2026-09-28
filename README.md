# PDVStore

![CI](https://github.com/marciodeandrade1/PDVStore-1.0/actions/workflows/ci.yml/badge.svg)

Sistema de Ponto de Venda (PDV) para pequenas lojas e comércios de bairro. Desenvolvido em **C# / .NET 8** com **WinForms**, **Entity Framework Core** e banco **SQL Server LocalDB**.

## Funcionalidades

- Vendas no PDV com **exigência de caixa aberto**
- Formas de pagamento: **Dinheiro, PIX, Cartão de Crédito, Cartão de Débito e Fiado**
- **Venda fiada** com clientes, limite de crédito e saldo devedor
- Cadastro de **produtos** com preço de custo e **estoque mínimo** (alertas de reposição)
- Gestão de **estoque** com entrada/saída manual e histórico de movimentações
- Cadastro de **fornecedores** e **compras** (entrada de mercadorias atualiza o estoque automaticamente)
- **Abertura e fechamento de caixa** com sangria e valor inicial
- **Dashboard e relatórios** (itens mais vendidos, totais por forma de pagamento e alertas de estoque) com exportação em **PDF e Excel**
- Login com usuários e **papéis (permissões)**: **Administrador**, **Operador (Caixa)** e **Estoquista**

### Papéis de acesso

| Papel | Acesso |
|---|---|
| **Administrador** | Todas as funcionalidades (PDV, dashboard/relatórios, produtos, estoque, clientes, fornecedores, compras, caixa e usuários) |
| **Operador (Caixa)** | Somente o **PDV** (vendas) |
| **Estoquista** | Somente o **cadastro de produtos** |

---

## Requisitos

- Windows 10/11
- [.NET SDK 8.0](https://dotnet.microsoft.com/pt-br/download) ou superior
- SQL Server **LocalDB** (`MSSQLLocalDB`) ou SQL Server/Express
- Ferramenta global do EF Core (para aplicar migrações):
  ```
  dotnet tool install --global dotnet-ef --version 9.0.17
  ```

---

## Instalação

### 0. Instalador guiado (PDVStore.Setup)

O projeto **`PDVStore.Setup`** é um assistente WinForms que **verifica requisitos, prepara o ambiente e instala o software** passo a passo:

1. **Boas-vindas** — escolha a pasta de instalação (publicação) e o nome do banco.
2. **Verificação** — detecta se já existem: **.NET SDK 8+**, **SQL Server Express LocalDB** e a ferramenta global **dotnet-ef**.
3. **Preparação (7 etapas)** — baixa e instala automaticamente o que estiver ausente (via **dotnet-install.ps1** oficial do .NET e do **SqlLocalDB.msi** oficial da Microsoft):
   - ① .NET SDK 8 (`%LOCALAPPDATA%\Microsoft\dotnet`, sem admin);
   - ② SQL Server Express LocalDB (pode pedir confirmação do UAC);
   - ③ dotnet-ef (`dotnet tool install --global dotnet-ef --version 9.0.*`);
   - ④ PATH do usuário (`.dotnet\tools` e pasta do SDK);
   - ⑤ instância **MSSQLLocalDB** (cria/inicia);
   - ⑥ `dotnet ef database update` (migrações);
   - ⑦ `dotnet publish` do PDVStore (Release) na pasta escolhida + atalho na área de trabalho.
4. **Conclusão** — resumo da instalação e opção de **abrir o PDVStore**.

Para executar (dentro da pasta do repo):

```bash
dotnet build PDVStore.Setup\PDVStore.Setup.csproj
dotnet run --project PDVStore.Setup\PDVStore.Setup.csproj
```

Caso o projeto PDVStore não seja localizado (ex.: instalador copiado sozinho), as etapas ⑥ e ⑦ são puladas com aviso — o banco é migrado automaticamente pelo próprio aplicativo na primeira execução.

### 1. Restaurar os pacotes

```bash
dotnet restore
```

### 2. Preparar o banco de dados

O sistema usa a base `PDV_StoreDB` na instância LocalDB padrão. A connection string fica concentrada em `Helpers/ConnectionHelper.cs`:

```
Server=(localdb)\MSSQLLocalDB;Database=PDV_StoreDB;Trusted_Connection=True;
```

Se precisar usar SQL Server/Express, basta alterar essa connection string — a mesma configuração vale para a execução e para o EF Core.

Inicie o LocalDB (caso necessário) e aplique as migrações:

```bash
sqllocaldb start MSSQLLocalDB
dotnet ef database update
```

> O comando cria as tabelas (`Usuarios`, `Produtos`, `Vendas`, `ItensVendas`, `MovimentacoesEstoque`, `Clientes`, `Caixas`, `Fornecedores`, `Compras`, `ItensCompras`) e os dados iniciais.

### 3. Executar

```bash
dotnet run
```

Ou abra a solution no Visual Studio e pressione **F5**.

### 4. Primeiro acesso

| Campo  | Valor      |
| ------ | ---------- |
| Usuário| `Admin`    |
| Senha  | `admin123` |

No primeiro acesso, **troque a senha** pelo menu **Usuários**. No bootstrap, se o hash da senha do administrador estiver inválido, o sistema restaura automaticamente a senha inicial `admin123`.

---

## Fluxo de operação

### 0. Inicialização (verificação)

Ao abrir o sistema, a tela **Splash** executa automaticamente a verificação pré-login:

1. **Configuração** — valida a connection string.
2. **Conexão** — testa o acesso ao banco (LocalDB).
3. **Migrações** — aplica qualquer migração pendente.
4. **Acesso administrativo** — garante que existe um administrador ativo com hash de senha válido.

Se tudo passar, abre a tela de **login**. Se qualquer etapa falhar, o erro é exibido e a aplicação é encerrada (verifique `logs/pdvstore-*.log`).

### 1. Login

- Informe usuário e senha (acesso padrão: `Admin` / `admin123`).
- O menu principal abre com os módulos do sistema. Bem-vindo com o nome do operador logado.

### 2. Abrir o caixa (obrigatório)

Para registrar vendas, o caixa precisa estar **aberto**:

1. Clique em **Abrir/Fechar Caixa**.
2. Informe o **valor inicial** (fundo de troco, pode ser `0`).
3. Enquanto o caixa estiver aberto, as vendas são vinculadas a ele.
   - **Sangria**: retire valores do caixa durante o expediente.
   - **Fechamento**: calcula `valor inicial + vendas − sangrias` e grava o valor final.

> Sem caixa aberto, o PDV bloqueia a finalização da venda.

### 3. Cadastrar produtos

Antes de vender, cadastre o catálogo em **Produtos**:

- Código de barras, nome, **preço de venda**, **preço de custo**, estoque, **estoque mínimo** (alerta de reposição), categoria e descrição.
- Gerencie estoque em **Estoque** (entrada/saída manual com motivo).
- Produtos desativados ficam ocultos das vendas.

### 4. Cadastrar clientes (venda fiada)

Em **Clientes**, informe nome, CPF/CNPJ, telefone, endereço e **limite de crédito**.

- Vendas fiadas **exigem um cliente ativo** e **respeitam o limite de crédito** (saldo + compra).
- Receba pagamentos parciais/totais do débito pelo botão **Receber débito**.

### 5. Vender no PDV

Na tela **Vender (PDV)**:

1. **Busque o produto** pelo nome ou código de barras (Enter ou botão Buscar).
2. Informe a **quantidade** e clique em **Adicionar**.
3. Repita até montar o carrinho (remova itens se necessário).
4. Selecione a **forma de pagamento**:
   - **Dinheiro** → informe o **valor recebido** e veja o **troco**.
   - **PIX / Cartão** → pagamento processado (PIX gera um TxId de referência).
   - **Fiado** → selecione o **cliente**; o débito é lançado no saldo devedor.
5. Aplique **desconto** se desejar.
6. Clique em **Finalizar venda**: o estoque é baixado, o caixa atualizado e a movimentação registrada automaticamente.

### 6. Registrar compras (entrada de mercadorias)

Em **Compras**:

1. Selecione o **fornecedor**, informe o número da nota.
2. Adicione os **itens** (produto, quantidade e preço de custo).
3. Salve: o estoque é **atualizado automaticamente** e o custo do produto é reaproveitado.
4. Para estornar, use **Cancelar compra selecionada** — o estoque é devolvido.

Cadastre fornecedores em **Fornecedores** antes de registrar compras.

### 7. Acompanhar o negócio

No **Dashboard & Relatórios**:

- Escolha o **período** e veja total e quantidade de vendas.
- Confira **itens mais vendidos** e **alertas de estoque mínimo**.
- Exporte os relatórios em **PDF** ou **Excel**.

### 8. Usuários e permissões

Em **Usuários** (somente Administrador cria/edita):

- **Operador (Caixa)**: acesso apenas ao PDV (vendas).
- **Estoquista**: acesso apenas ao cadastro de produtos.
- **Administrador**: acesso total (todas as telas, incluindo usuários).
- Cadastro de novo usuário exige nome, senha (mín. 6 caracteres), confirmação e **papel**. Foto opcional.

---

## CI — Integração Contínua (GitHub Actions)

A **Integração Contínua** (CI) valida automaticamente o código **sempre que alguém fizer uma alteração**, avisando cedo se quebrar build ou testes. A ferramenta nativa usada é o **GitHub Actions**, declarada no arquivo `.github/workflows/ci.yml`.

O status da última execução aparece no **badge** no topo deste README: ✔ verde (sucesso), ❌ vermelho (falha), ● amarelo (em execução).

### O que a CI verifica a cada execução

| # | Etapa | Comando | Por quê |
|---|-------|---------|---------|
| 1 | Instala o SDK .NET 8 | `actions/setup-dotnet` | Garante a mesma versão do .NET para todos os colaboradores |
| 2 | Compila o aplicativo principal | `dotnet build PDVStore.csproj --configuration Release` | Detecta erros de compilação/incompatibilidades |
| 3 | Compila o instalador | `dotnet build PDVStore.Setup/PDVStore.Setup.csproj --configuration Release` | O instalador não pode ficar quebrado |
| 4 | Verifica migrações pendentes | `dotnet ef migrations has-pending-model-changes` | Toda mudança no modelo (banco) precisa ter migration |
| 5 | Executa os testes | `dotnet test PDVStore.Tests/PDVStore.Tests.csproj --configuration Release` | Roda os 165 testes NUnit do projeto |

> A CI roda no runner **Windows** (`windows-latest`) porque os projetos usam WinForms (`net8.0-windows7.0`).

### Quando a CI é disparada

- **Push** para as branches `master` ou `main`;
- **Pull Request** aberto/atualizado voltado para `master` ou `main`;
- **Manualmente** (botão *Run workflow*, ver abaixo).
- Alterações **somente** de arquivos `.md` ou do `LICENSE.txt` **não** disparam a CI.

### Como acompanhar uma execução (passo a passo)

1. Acesse o repositório e clique na aba **Actions** (no topo da página).
2. No menu lateral esquerdo, selecione o workflow **CI** para ver o histórico de execuções.
3. Clique na execução mais recente (o push/PR que você ou um colega fez).
4. Você verá o job **"Build e Testes (.NET 8 / Windows)"** — clique nele.
5. Um detalhamento com as 5 etapas da tabela acima é exibido; clique em qualquer etapa para abrir os **logs** completos (console colorido).
6. Resultado da execução:
   - ✔ **Verde** — tudo passou;
   - ❌ **Vermelho** — alguma etapa falhou (clique na etapa vermelha para ver o erro);
   - ● **Amarelo/círculo** — ainda em execução.

### Rodar a CI manualmente

1. Aba **Actions** → workflow **CI** → botão **"Run workflow"** (lado direito).
2. Selecione a branch (ex.: `master`) → **Run workflow**.
3. Acompanhe a nova execução conforme os passos acima.

### Reexecutar uma execução com falha

Em qualquer execução, clique em **"Re-run jobs"** (ou *Re-run all jobs*) no canto superior direito — útil quando a falha é temporária (ex.: instalação de dependência).

### Boas práticas para colaboradores

Antes de abrir um PR ou dar push, rode **localmente as mesmas etapas** para não depender só da CI:

```bash
dotnet build PDVStore.csproj --configuration Release
dotnet build PDVStore.Setup/PDVStore.Setup.csproj --configuration Release
dotnet test PDVStore.Tests/PDVStore.Tests.csproj --configuration Release
dotnet ef migrations has-pending-model-changes --project PDVStore.csproj
```

- Se você **alterar o modelo** (entidades/`OnModelCreating`) e esquecer a migration, a etapa 4 falha — crie uma com `dotnet ef migrations add <Nome>`.
- Se um **teste falhar**, abra os logs na etapa 5, leia o cenário e a regra de negócio descrita nos comentários do teste, e corrija antes do merge.
- PRs de **forks** também passam pela CI automaticamente na aba *Checks* do próprio PR.

---

## Estrutura do projeto

| Pasta        | Descrição                                                                 |
| ------------ | ------------------------------------------------------------------------- |
| `Models`     | Entidades do banco, login e permissões de usuário                         |
| `Data`       | `PDVContext` (DbContext), migrações e seeds                               |
| `Services`   | Regras de negócio: venda, estoque, caixa, clientes, fornecedores, compras, relatórios e integrações (mock PIX/cartão) |
| `ViewModels` | Dados exibidos nas telas (Dashboard, PDV)                                 |
| `Forms`      | Telas de login, menu e operação (PDV, cadastros, caixa, dashboard)        |
| `Helpers`    | Connection string centralizada e utilitários (prompts, validações)        |

---

## Observações

- A integração de pagamento (PIX/cartão) é um **mock didático**: o valor é validado e o PIX registra um `TxId`, sem conexão com adquirentes reais.
- O log da aplicação fica em `logs/pdvstore-yyyyMMdd.log` (Serilog).
- Licença: **MIT** — consulte o arquivo `LICENSE.txt` (Copyright © 2026 Marcio de Andrade).