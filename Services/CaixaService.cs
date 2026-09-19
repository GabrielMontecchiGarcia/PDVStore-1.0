using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class CaixaService
    {
        private readonly PDVContext _context;

        // CONSTRUTOR: recebe o PDVContext por injeção de dependência e o
        // armazena no campo _context. É a única forma de o serviço acessar o
        // banco de dados; todos os métodos abaixo dependem desse contexto para
        // consultar e gravar caixas. Quem o chama: o container de DI da
        // aplicação ao instanciar CaixaService para outros serviços/formulários.
        // Não lança exceções; apenas guarda a dependência recebida.
        public CaixaService(PDVContext context)
        {
            _context = context;
        }

        // Abre um novo caixa no PDV. Regra de negócio importante: só pode existir
        // UM caixa aberto por vez. O método consulta ObterCaixaAbertoAsync e, se
        // já existir um aberto, lança InvalidOperationException (decisão comum em
        // PDV para evitar vendas em dois caixas simultâneos). Cria a entidade
        // Caixa com status "Aberto", valor inicial, usuário responsável e data de
        // abertura UTC, e grava via SaveChangesAsync. Quem chama: a tela de
        // abertura de caixa (após o turno iniciar). Retorna o caixa criado.
        public async Task<Caixa> AbrirCaixaAsync(decimal valorInicial, int usuarioCaixaId)
        {
            var existente = await ObterCaixaAbertoAsync();
            if (existente != null)
                throw new InvalidOperationException("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.");

            var caixa = new Caixa
            {
                UsuarioCaixaId = usuarioCaixaId,
                Abertura = DateTime.UtcNow,
                ValorInicial = valorInicial,
                Status = "Aberto"
            };

            _context.Caixas.Add(caixa);
            await _context.SaveChangesAsync();
            return caixa;
        }

        // Consulta o caixa que está "Aberto" no momento. Usa AsNoTracking porque é
        // uma leitura apenas de verificação (EF não precisa rastrear a entidade).
        // Retorna null se não houver caixa aberto. Além de servir à abertura de
        // caixa, este método é DEPENDÊNCIA de VendaService, que exige um caixa
        // aberto antes de registrar qualquer venda (regra de domínio do PDV).
        public async Task<Caixa?> ObterCaixaAbertoAsync()
        {
            return await _context.Caixas
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Status == "Aberto");
        }

        // Fecha um caixa aberto, executado ao final do turno. Regras: o caixa deve
        // existir e estar "Aberto"; caso contrário retorna false sem lançar erro.
        // O valor final segue a fórmula de fechamento:
        //   ValorInicial + soma das vendas CONCLUÍDAS do caixa - sangrias.
        // Depende de Vendas com Status == "Concluida" vinculadas ao caixaId.
        // Grava valor final, data de fechamento (UTC) e status "Fechado".
        // Quem chama: a tela de fechamento, após a conferência do dinheiro.
        public async Task<bool> FecharCaixaAsync(int caixaId)
        {
            var caixa = await _context.Caixas.FindAsync(caixaId);

            if (caixa == null || caixa.Status != "Aberto")
                return false;

            decimal totalVendas = await _context.Vendas
                .Where(v => v.CaixaId == caixaId && v.Status == "Concluida")
                .SumAsync(v => v.ValorTotal);
            decimal sangria = caixa.Sangria ?? 0;

            caixa.ValorFinal = caixa.ValorInicial + totalVendas - sangria;
            caixa.Fechamento = DateTime.UtcNow;
            caixa.Status = "Fechado";

            await _context.SaveChangesAsync();
            return true;
        }

        // Registra uma sangria, ou seja, a retirada de dinheiro do caixa durante o
        // turno (ex.: pagar uma despesa ou depositar em banco). Regras: o valor
        // deve ser maior que zero (senão ArgumentException) e o caixa deve
        // existir e estar "Aberto" (senão retorna false). O valor é ACUMULADO em
        // Sangria e, para que o célculo do fechamento não falhe, garante que
        // ValorFinal já possua um valor inicial. Persiste e retorna true.
        public async Task<bool> RegistrarSangriaAsync(int caixaId, decimal valor)
        {
            if (valor <= 0)
                throw new ArgumentException("Valor da sangria deve ser maior que zero.", nameof(valor));

            var caixa = await _context.Caixas.FindAsync(caixaId);
            if (caixa == null || caixa.Status != "Aberto")
                return false;

            caixa.Sangria = (caixa.Sangria ?? 0) + valor;
            caixa.ValorFinal = caixa.ValorFinal ?? caixa.ValorInicial;
            await _context.SaveChangesAsync();
            return true;
        }

        // Lista todos os caixas, do mais recente para o mais antigo (OrderByDescending
        // em Abertura), trazendo também o usuário responsável (Include
        // UsuarioCaixa) para exibição em histórico. Usa AsNoTracking por ser
        // consulta de leitura. Não filtra por status: retorna caixas abertos e
        // fechados. Quem chama: telas de histórico/relatório de caixas.
        public async Task<List<Caixa>> ListarAsync()
        {
            return await _context.Caixas
                .Include(c => c.UsuarioCaixa)
                .AsNoTracking()
                .OrderByDescending(c => c.Abertura)
                .ToListAsync();
        }
    }
}