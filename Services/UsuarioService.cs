using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PDVStore.Data;
using PDVStore.Models;

namespace PDVStore.Services
{
    public class UsuarioService
    {
        private readonly PDVContext _context;

        public UsuarioService(PDVContext context)
        {
            _context = context;
        }

        // ====================== CRUD ======================

        public async Task<UsuarioCaixa> CriarAsync(UsuarioCaixa usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nome))
                throw new ArgumentException("Nome é obrigatório.");

            usuario.SetSenha(usuario.SenhaHash); // Garante hash (ajuste se receber senha limpa)
            usuario.CreatedAt = DateTime.UtcNow;
            usuario.SetAtivo(true);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<UsuarioCaixa?> ObterPorIdAsync(int id)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<UsuarioCaixa?> ObterPorNomeAsync(string nome)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Nome == nome);
        }

        public async Task<IEnumerable<UsuarioCaixa>> ListarTodosAsync(bool apenasAtivos = true)
        {
            var query = _context.Usuarios.AsNoTracking();

            if (apenasAtivos)
                query = query.Where(u => u.Ativo);

            return await query.ToListAsync();
        }

        public async Task AtualizarAsync(UsuarioCaixa usuario)
        {
            var existing = await _context.Usuarios.FindAsync(usuario.Id);
            if (existing == null)
                throw new KeyNotFoundException("Usuário não encontrado.");

            // Atualiza campos permitidos
            existing.Nome = usuario.Nome;
            existing.Permissao = usuario.Permissao;
            existing.SetAtivo(usuario.GetAtivo());
            existing.SetFotoPath(usuario.GetFotoPath());

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeletarAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.SetAtivo(false); // Soft delete
            await _context.SaveChangesAsync();
            return true;
        }

        // ====================== Autenticação ======================

        public async Task<UsuarioCaixa?> AutenticarAsync(string nome, string senha)
        {
            var usuario = await ObterPorNomeAsync(nome);
            if (usuario == null || !usuario.Autenticar(senha))
                return null;

            return usuario;
        }

        public async Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null || !usuario.Autenticar(senhaAtual))
                return false;

            usuario.SetSenha(novaSenha);
            await _context.SaveChangesAsync();
            return true;
        }

        // ====================== Permissões ======================

        public async Task<bool> EhAdministradorAsync(int id)
        {
            var usuario = await ObterPorIdAsync(id);
            return usuario?.EhAdmin() == true;
        }

        public async Task<IEnumerable<UsuarioCaixa>> ListarAdministradoresAsync()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Permissao == TipoPermissao.Administrador)
                .ToListAsync();
        }

        public T? GetById<T>(int id) where T : class, IHasId
        {
            return _context.Set<T>().Find(id);
        }
    }
}