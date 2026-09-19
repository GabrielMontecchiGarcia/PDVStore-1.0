# PDVStore

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
- Login com usuários e permissões (**Operador / Administrador**)

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

- **Operador**: vende e vê relatórios básicos.
- **Administrador**: gerencia usuários, produtos e configurações.
- Cadastro de novo usuário exige nome, senha (mín. 6 caracteres) e confirmação. Foto opcional.

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
- O `LICENSE.txt` ainda possui placeholders a serem preenchidos pelo proprietário.