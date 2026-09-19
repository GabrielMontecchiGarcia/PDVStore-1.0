# PDV Store — Manual de Operação

Guia de uso do sistema para os perfis **Administrador**, **Estoquista** e **Caixa (Operador)**.

---

## 1. Sobre o sistema

O PDV Store é um sistema de ponto de venda para Windows que controla:

- Vendas (dinheiro, PIX, cartão de crédito, cartão de débito e fiado);
- Abertura, sangria e fechamento de caixa;
- Cadastro de produtos, clientes, fornecedores e usuários;
- Entrada e saída de estoque (manual e automática);
- Compras (entrada de mercadorias);
- Dashboard e relatórios com exportação em PDF e Excel.

Ao iniciar o aplicativo, uma tela de verificação (splash) confere automaticamente a
configuração, a conexão com o banco de dados (SQL Server LocalDB), as migrações pendentes
e o acesso administrativo antes de liberar o login.

Logs de erros são gravados em `logs/pdvstore-AAAAMMDD.log` na pasta do sistema.

---

## 2. Acesso ao sistema

1. Informe o **usuário** e a **senha**.
2. Clique em **Entrar** (ou botão equivalente de login).
3. O menu principal exibe apenas as opções permitidas para o seu perfil.

> **Primeiro acesso:** o sistema cria automaticamente o usuário `Admin` com senha
> `admin123`. **Troque a senha imediatamente** após o primeiro acesso.

### 2.1 Perfis de acesso

| Perfil | Menu exibido | Descrição |
| ------ | ------------ | --------- |
| **Administrador** | Todas as opções | Acesso total: PDV, Dashboard, Produtos, Estoque, Clientes, Fornecedores, Compras, Caixa e Usuários |
| **Estoquista** | Produtos | Acesso somente ao cadastro de produtos |
| **Caixa (Operador)** | Vender (PDV) | Acesso somente ao ponto de venda |

---

## 3. Perfil Caixa (Operador)

O operador de caixa é responsável exclusivamente pelas **vendas**.

### 3.1 Antes de vender

- O **caixa deve estar aberto**. Somente o Administrador pode abrir o caixa.
- Se o caixa estiver fechado, o PDV exibe `Caixa: fechado. Abra o caixa para vender.`
  e o botão **Finalizar venda** fica desabilitado.

### 3.2 Como registrar uma venda

1. No menu principal, clique em **Vender (PDV)**.
2. Localize o produto:
   - Digite o nome ou o código de barras em **Buscar** e pressione `Enter`; ou
   - Selecione o produto diretamente na lista.
3. Informe a **Qtd** (quantidade) desejada.
4. Clique em **Adicionar (Enter)** — o item aparece no carrinho.
5. Ao final, no grupo **Pagamento**:
   - Escolha a **forma de pagamento**;
   - Aplique o **desconto (R$)**, se houver;
   - Para **Dinheiro**, informe o **valor recebido** e confira o **troco**;
   - Se a venda for identificada (ou fiada), selecione o **cliente**.
6. Clique em **Finalizar venda**.

### 3.3 Formas de pagamento

| Forma | Observações |
| ----- | ----------- |
| **Dinheiro** | Obrigatório informar valor recebido; o sistema calcula o troco e bloqueia venda se o valor for insuficiente |
| **PIX** | Mock de integração; o sistema gera um identificador de transação (TxId) |
| **Cartão Crédito / Débito** | Processamento simulado |
| **Fiado** | Exige **cliente selecionado** e respeita o **limite de crédito** do cliente |

### 3.4 Venda fiada (caderneta)

- É **obrigatório** selecionar um cliente no campo **Cliente**.
- O valor da venda é somado ao **saldo devedor** do cliente.
- Se o cliente possui **limite de crédito** definido (`Limite > 0`), a venda é
  bloqueada quando `débito atual + valor da venda` ultrapassar o limite.
- **Limite igual a zero significa que não há limite configurado** para bloqueio.
- Clientes inativos não podem receber venda fiada.

### 3.5 Manipulando o carrinho

- **Remover item**: selecione o item no carrinho e clique em **Remover item**.
- **Limpar venda**: apaga todos os itens e zera desconto.
- O carrinho mostra o **nome do produto**, quantidade, preço unitário e subtotal.

---

## 4. Perfil Estoquista

O estoquista é responsável pelo **cadastro de produtos**.

No menu principal, clique em **Produtos**.

### 4.1 Cadastrar produto

1. Clique em **Novo** para limpar os campos.
2. Preencha:
   - **Código Barras** (obrigatório);
   - **Produto** / nome (obrigatório);
   - **Preço Venda** (obrigatório, não pode ser negativo);
   - **Preço Custo**;
   - **Estoque** atual;
   - **Estoque Mínimo** (usado no alerta de reposição);
   - **Categoria**;
   - **Descrição**.
3. Clique em **Salvar**.

### 4.2 Editar e desativar

- Selecione o produto na lista para carregar os dados e clique em **Salvar** após editar.
- **Excluir** desativa o produto (soft delete): ele some da lista de venda, mas o
  registro permanece no banco.

### 4.3 Ajuste de estoque

O ajuste manual de estoque (entrada/saída com motivo) é feito na tela **Estoque**,
disponível para o **Administrador**. O botão **Estoque** da tela de produtos apenas
orienta a usar essa tela.

> Dica: mantenha o **estoque mínimo** atualizado para que o Dashboard alerte a
> reposição corretamente.

---

## 5. Perfil Administrador

O administrador tem acesso a **todas** as telas: Vender (PDV), Dashboard, Produtos,
Estoque, Clientes, Fornecedores, Compras, Abrir/Fechar Caixa, Usuários e Sobre.

### 5.1 Abrir / Fechar Caixa

Tela **Abrir/Fechar Caixa**:

- **Abrir caixa**: informa o **valor inicial (fundo de troco)**. Enquanto houver um
  caixa aberto, não é possível abrir outro.
- **Sangria**: registra a retirada de valores do caixa (despesas, troco extra etc.).
- **Fechar caixa**: o sistema calcula o **valor final** como
  `valor inicial + vendas concluídas − sangrias`.
- O **histórico** de caixas (abertura, fechamento, inicial, final, sangria) pode ser
  exportado em **PDF** e **Excel**.

> As vendas do PDV **somente são permitidas com caixa aberto**.

### 5.2 Vender (PDV)

Mesmo fluxo do perfil **Caixa** (veja seção 3). O administrador também pode entregar
vendas como forma de contingência.

### 5.3 Dashboard e relatórios

- Selecione o período (**De / Até**) e clique em **Atualizar**.
- Resumo do período: total vendido, quantidade de vendas e formas de pagamento.
- **Itens mais vendidos** (top 10 por quantidade e valor).
- **Alertas de estoque mínimo** (produtos com estoque ≤ mínimo).
- Gráficos: **Entradas e Saídas de Produtos** e **Vendas por Dia**.
- **Exportar PDF / Excel**: gera o relatório de itens mais vendidos e a lista de
  alertas de estoque.

### 5.4 Produtos

Mesmas operações do perfil **Estoquista** (cadastrar, editar, desativar) e exportação
em PDF/Excel.

### 5.5 Estoque

Tela **Gestão de Estoque — Entrada/Saída**:

1. Selecione um produto na lista.
2. Escolha o **tipo** do movimento: **Entrada** ou **Saída**.
3. Informe a **quantidade** e, opcionalmente, o **motivo**.
4. Clique em **Confirmar movimento**.

O sistema valida a quantidade (não permite saída maior que o estoque disponível) e
registra o **histórico de movimentações** automaticamente.

### 5.6 Clientes (fiado / caderneta)

Tela **Clientes (Fiado / Caderneta)**:

- **Cadastrar**: nome (obrigatório), CPF/CNPJ (formatação automática e validação),
  telefone, e-mail, endereço e **limite de crédito (R$)**.
- **Editar**: carrega os dados do cliente na grade; somente os campos alterados são
  validados novamente.
- **Desativar**: inativa o cliente (não pode mais receber venda fiada).
- **Receber débito**: baixa o saldo devedor do cliente.
  - Aceita **pagamento parcial** (valor menor que o débito);
  - Se o valor informado for **maior** que o débito, pergunta se deseja registrar
    apenas o valor devido (quitando o débito);
  - Ao final é exibido o saldo restante.
- Exportação do cadastro em PDF/Excel.

> O **saldo devedor** também aumenta automaticamente a cada **venda fiada** e é
> devolvido (reduzido) quando a venda é cancelada pelo sistema.

### 5.7 Fornecedores

- **Cadastrar**: nome (obrigatório), CNPJ, telefone e e-mail (com máscaras e validação).
- **Desativar**: inativa o fornecedor.
- Exportação em PDF/Excel.

### 5.8 Compras (entrada de mercadorias)

Tela **Compras / Entrada de Mercadorias**:

1. Selecione o **fornecedor**.
2. Informe o **Nº da nota** (opcional).
3. Selecione o **produto**, a **quantidade** e o **custo unitário**.
4. Clique em **Adicionar item** e repita para cada produto da nota.
5. Confira o **total** e clique em **Salvar compra**.

A compra **dá entrada automática no estoque** dos itens e registra as movimentações.
Para cancelar, selecione a compra na listagem e clique em **Cancelar compra
selecionada** — o estoque é **estornado** (devolvido).

### 5.9 Usuários

Tela de **gerenciamento de usuários**:

- **Novo/Editar**: cadastro com nome, senha e **permissão** — Administrador,
  Operador (Caixa) ou Estoquista — e opção de **foto**.
- **Excluir**: desativa o usuário.
- Busca por nome e exportação em PDF/Excel.

---

## 6. Regras de negócio resumidas

| Regra | Comportamento |
| ----- | ------------- |
| **Venda sem caixa aberto** | Bloqueada (mensagem para abrir o caixa) |
| **Venda sem itens** | Bloqueada |
| **Estoque insuficiente** | Bloqueada (por item) |
| **Dinheiro com valor recebido insuficiente** | Bloqueada |
| **Fiado sem cliente selecionado** | Bloqueada |
| **Fiado para cliente inativo** | Bloqueada |
| **Fiado acima do limite de crédito** | Bloqueada |
| **Sangria com valor ≤ 0** | Rejeitada |
| **Nova abertura com caixa aberto** | Rejeitada |
| **Saída de estoque maior que o disponível** | Rejeitada |
| **Compra sem itens / item com qtd ≤ 0 / custo negativo** | Rejeitada |
| **Exclusões** | Sempre **soft delete** (desativação) |

---

## 7. Solução de problemas

| Problema | Causa provável | Como resolver |
| -------- | -------------- | ------------- |
| Não consigo finalizar venda | Caixa fechado | Peça ao Administrador para **Abrir caixa** |
| Mensagem "Já existe um caixa aberto" | Caixa não fechado | Feche o caixa atual antes de abrir outro |
| Crédito insuficiente para fiado | Débito + venda > limite | Receba parte do débito em **Clientes** ou reduza o valor da venda |
| Não consigo salvar cliente com CPF/CNPJ | Documento inválido | Confira os dígitos (CPF 11, CNPJ 14) |
| Erro de conexão com o banco | SQL Server LocalDB indisponível | Verifique o serviço `MSSQLLocalDB` ou reinstale o LocalDB |
| Após algum erro, app não abre | Falha na verificação inicial | Consulte os logs em `logs/pdvstore-*.log` e contate o suporte |

---

Para dúvidas técnicas ou erros de funcionamento, informe o conteúdo dos arquivos de
log da pasta `logs/`.