using PDVStore.Data;
using PDVStore.Models;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PDVStore.Forms
{
    public partial class frmGerenciarUsuarios : Form
    {
        private readonly PDVContext _context;
        private readonly IServiceProvider _serviceProvider;

        public frmGerenciarUsuarios(PDVContext context, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _context = context;
            _serviceProvider = serviceProvider;

            ConfigurarGrid();
            CarregarUsuarios();

            txtBusca.TextChanged += TxtBusca_TextChanged;
        }

        private void ConfigurarGrid()
        {
            dgvUsuarios.AutoGenerateColumns = false;
            dgvUsuarios.RowTemplate.Height = 60; // Aumenta altura da linha para melhor visualização da foto

            // Colunas
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", Width = 60 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nome", HeaderText = "Nome de Usuário", DataPropertyName = "Nome", Width = 180 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Permissao", HeaderText = "Permissão", DataPropertyName = "Permissao", Width = 130 });
            dgvUsuarios.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 90 });

            // === COLUNA FOTO (Image Column) ===
            var fotoColumn = new DataGridViewImageColumn
            {
                Name = "Foto",
                HeaderText = "Foto",
                DataPropertyName = "Foto",
                Width = 80,
                ImageLayout = DataGridViewImageCellLayout.Zoom,   // Mantém proporção
                DefaultCellStyle = { NullValue = Properties.Resources.user_default }
            };
            dgvUsuarios.Columns.Add(fotoColumn);
        }

        private void CarregarUsuarios(string filtro = "")
        {
            var query = _context.Usuarios.Where(u => u.Ativo);

            if (!string.IsNullOrWhiteSpace(filtro))
                query = query.Where(u => u.Nome.Contains(filtro));

            var usuarios = query
                .Select(u => new { u.Id, u.Nome, u.Permissao, u.Ativo, u.FotoPath })
                .AsEnumerable() // materializa a consulta antes de realizar I/O de arquivos
                .Select(u => new
                {
                    u.Id,
                    u.Nome,
                    Permissao = UsuarioCaixa.DescreverPermissao(u.Permissao),
                    Status = u.Ativo ? "Ativo" : "Inativo",
                    Foto = ObterImagemUsuario(u.FotoPath)   // carregamento seguro em memória
                })
                .ToList();

            dgvUsuarios.DataSource = usuarios;
        }

        // Método seguro e otimizado para carregar imagem
        private Image ObterImagemUsuario(string? fotoPath)
        {
            if (string.IsNullOrEmpty(fotoPath) || !File.Exists(fotoPath))
                return Properties.Resources.user_default;

            try
            {
                using var fs = new FileStream(fotoPath, FileMode.Open, FileAccess.Read);
                using var tempImage = Image.FromStream(fs);
                return new Bitmap(tempImage); // Cópia em memória para evitar lock
            }
            catch
            {
                return Properties.Resources.user_default;
            }
        }

        private void TxtBusca_TextChanged(object? sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        // ====================== BOTÕES ======================
        private void btnNovo_Click(object sender, EventArgs e)
        {
            var frm = new frmCadastroUsuario(_context);
            frm.ShowDialog();
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.CurrentRow == null) return;

            int id = Convert.ToInt32(dgvUsuarios.CurrentRow.Cells["Id"].Value);
            var usuario = _context.Usuarios.Find(id);

            if (usuario != null)
            {
                var frm = new frmCadastroUsuario(_context, usuario);
                frm.ShowDialog();
                CarregarUsuarios(txtBusca.Text.Trim());
            }
        }

        private async void btnExcluir_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.CurrentRow == null) return;

            if (MessageBox.Show("Deseja desativar este usuário?", "Confirmação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int id = Convert.ToInt32(dgvUsuarios.CurrentRow.Cells["Id"].Value);
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario != null)
            {
                usuario.SetAtivo(false);
                await _context.SaveChangesAsync();
                Log.Information("Usuário desativado: {Nome}", usuario.Nome);
                CarregarUsuarios(txtBusca.Text.Trim());
            }
        }

        private void btnAtualizar_Click(object sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        private void dgvUsuarios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                btnEditar.PerformClick();
        }

        // Event handler required by Designer. If not used, keep empty to satisfy wiring.
        private void dgvUsuarios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // No-op
        }

        // Designer wiring expects this handler. Forward to the existing implementation.
        private void btnExcluir_ClickAsync(object sender, EventArgs e)
        {
            // Forward to existing async handler
            btnExcluir_Click(sender, e);
        }

        // Designer wiring expects this handler for search button.
        private void btnBuscarUsuario_Click(object sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }
    }
}