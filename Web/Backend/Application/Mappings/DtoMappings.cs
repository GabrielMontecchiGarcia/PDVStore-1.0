using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Application.Mappings;

/// <summary>
/// Conversões entidade → DTO. Centralizadas na camada Application para que a API
/// nunca exponha entidades de persistência diretamente (DTO Pattern).
/// Nenhuma regra de negócio é aplicada aqui; apenas projeção de dados.
/// </summary>
public static class DtoMappings
{
    public static UsuarioDto ToDto(this UsuarioCaixa usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Permissao = usuario.Permissao.ToString(),
        PermissaoCodigo = (int)usuario.Permissao,
        PermissaoDescricao = UsuarioCaixa.DescreverPermissao(usuario.Permissao),
        FotoPath = usuario.FotoPath,
        Ativo = usuario.Ativo,
        CreatedAt = usuario.CreatedAt
    };

    public static ProdutoDto ToDto(this Produto produto) => new()
    {
        Id = produto.Id,
        Nome = produto.Nome,
        CodigoBarras = produto.CodigoBarras,
        Preco = produto.Preco,
        PrecoCusto = produto.PrecoCusto,
        Estoque = produto.Estoque,
        EstoqueAtual = produto.EstoqueAtual,
        EstoqueMinimo = produto.EstoqueMinimo,
        Categoria = produto.Categoria,
        Descricao = produto.Descricao,
        Ativo = produto.Ativo
    };

    public static ClienteDto ToDto(this Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nome = cliente.Nome,
        CpfCnpj = cliente.CpfCnpj,
        Telefone = cliente.Telefone,
        Email = cliente.Email,
        Endereco = cliente.Endereco,
        CadastradoEm = cliente.CadastradoEm,
        Ativo = cliente.Ativo,
        LimiteCredito = cliente.LimiteCredito,
        SaldoDevedor = cliente.SaldoDevedor
    };

    public static FornecedorDto ToDto(this Fornecedor fornecedor) => new()
    {
        Id = fornecedor.Id,
        Nome = fornecedor.Nome,
        Cnpj = fornecedor.Cnpj,
        InscricaoEstadual = fornecedor.InscricaoEstadual,
        Telefone = fornecedor.Telefone,
        Email = fornecedor.Email,
        Ativo = fornecedor.Ativo
    };

    public static CaixaDto ToDto(this Caixa caixa) => new()
    {
        Id = caixa.Id,
        UsuarioCaixaId = caixa.UsuarioCaixaId,
        UsuarioNome = caixa.UsuarioCaixa?.Nome,
        Abertura = caixa.Abertura,
        Fechamento = caixa.Fechamento,
        ValorInicial = caixa.ValorInicial,
        ValorFinal = caixa.ValorFinal,
        Sangria = caixa.Sangria,
        Status = caixa.Status
    };

    public static MovimentacaoEstoqueDto ToDto(this MovimentacaoEstoque movimentacao) => new()
    {
        Id = movimentacao.Id,
        ProdutoId = movimentacao.ProdutoId,
        ProdutoNome = movimentacao.Produto?.Nome,
        Tipo = movimentacao.Tipo,
        Quantidade = movimentacao.Quantidade,
        PrecoUnitario = movimentacao.PrecoUnitario,
        DataMovimentacao = movimentacao.DataMovimentacao,
        UsuarioId = movimentacao.UsuarioId,
        UsuarioNome = movimentacao.Usuario?.Nome,
        Motivo = movimentacao.Motivo,
        ReferenciaVendaId = movimentacao.ReferenciaVendaId
    };

    public static ItemVendaDto ToDto(this ItemVenda item) => new()
    {
        Id = item.Id,
        ProdutoId = item.ProdutoId,
        ProdutoNome = item.Produto?.Nome ?? item.NomeProduto,
        Quantidade = item.Quantidade,
        PrecoUnitario = item.PrecoUnitario,
        Subtotal = item.Subtotal
    };

    public static VendaDto ToDto(this Venda venda) => new()
    {
        Id = venda.Id,
        UsuarioCaixaId = venda.UsuarioCaixaId,
        UsuarioNome = venda.UsuarioCaixa?.Nome,
        CaixaId = venda.CaixaId,
        ClienteId = venda.ClienteId,
        ClienteNome = venda.Cliente?.Nome,
        DataVenda = venda.DataVenda,
        ValorTotal = venda.ValorTotal,
        Desconto = venda.Desconto,
        FormaPagamento = venda.FormaPagamento,
        PixTxId = venda.PixTxId,
        Status = venda.Status,
        Itens = venda.Itens.Select(i => i.ToDto()).ToList()
    };

    public static ItemCompraDto ToDto(this ItemCompra item) => new()
    {
        Id = item.Id,
        ProdutoId = item.ProdutoId,
        ProdutoNome = item.Produto?.Nome,
        Quantidade = item.Quantidade,
        PrecoCusto = item.PrecoCusto,
        Subtotal = item.Subtotal
    };

    public static CompraDto ToDto(this Compra compra) => new()
    {
        Id = compra.Id,
        FornecedorId = compra.FornecedorId,
        FornecedorNome = compra.Fornecedor?.Nome,
        UsuarioCaixaId = compra.UsuarioCaixaId,
        UsuarioNome = compra.UsuarioCaixa?.Nome,
        DataCompra = compra.DataCompra,
        NumeroNota = compra.NumeroNota,
        ValorTotal = compra.ValorTotal,
        Status = compra.Status,
        Itens = compra.Itens.Select(i => i.ToDto()).ToList()
    };
}
