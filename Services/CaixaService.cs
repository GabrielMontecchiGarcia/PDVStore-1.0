using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class CaixaService
    {
        private readonly PDVContext _context;

        public CaixaService(PDVContext context)
        {
            _context = context;
        }

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

        public async Task<Caixa?> ObterCaixaAbertoAsync()
        {
            return await _context.Caixas
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Status == "Aberto");
        }

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