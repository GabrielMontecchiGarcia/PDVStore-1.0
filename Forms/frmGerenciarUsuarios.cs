using PDVStore.Data;
using PDVStore.Models;
using PDVStore.Services;
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

        // Construtor da tela de gestão de usuários.
        // O QUE FAZ: recebe o PDVContext (banco) e o IServiceProvider (para abrir telas
        // com DI), monta a grade, carrega a lista inicial e associa os hooks dos botões
        // de exportação (PDF/Excel) e do campo de busca.
        // POR QUE EXISTE: é o ponto de entrada que conecta a tela ao contexto do banco
        // e prepara a interface para listar e filtrar usuários.
        // QUEM CHAMA: resolvido pelo container DI (frmMenuPrincipal.OnUsuarios).
        public frmGerenciarUsuarios(PDVContext context, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _context = context;
            _serviceProvider = serviceProvider;

            ConfigurarGrid();
            CarregarUsuarios();

            var btnExportarPdf = new Button { Text = "Exportar PDF", Location = new Point(691, 290), Size = new Size(125, 29), FlatStyle = FlatStyle.Flat };
            var btnExportarExcel = new Button { Text = "Exportar Excel", Location = new Point(691, 325), Size = new Size(125, 29), FlatStyle = FlatStyle.Flat };
            btnExportarPdf.Click += (_, _) => ExportadorService.ExportarPdf(dgvUsuarios, "Usuários", $"Usuarios_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
            btnExportarExcel.Click += (_, _) => ExportadorService.ExportarExcel(dgvUsuarios, "Usuários", $"Usuarios_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            this.Controls.Add(btnExportarPdf);
            this.Controls.Add(btnExportarExcel);

            txtBusca.TextChanged += TxtBusca_TextChanged;
        }

        // Configura as colunas do DataGridView de usuários, incluindo a coluna de foto.
        // O QUE FAZ: desliga a geração automática de colunas, define altura da linha e
        // cria colunas para Id, Nome, Permissão, Status e Foto (com imagem default).
        // POR QUE EXISTE: a grade é apenas leitura e reflete o modelo Usuario; a coluna
        // Foto usa Zoom para preservar a proporção da imagem carregada.
        // QUEM CHAMA: o construtor da tela.
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
            foreach (DataGridViewColumn c in dgvUsuarios.Columns) c.FillWeight = Math.Max(50, c.Width);
            dgvUsuarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Consulta e exibe os usuários ativos, com filtro opcional pelo nome.
        // O QUE FAZ: monta uma query no PDVContext (somente usuários Ativos), aplica o
        // filtro de nome quando informado, traduz a permissão/status para texto amigável
        // e carrega a foto de cada um; o resultado vira o DataSource da grade.
        // POR QUE EXISTE: centraliza a listagem e a busca, usada no construtor, no
        // TextChanged da busca e após cada operação de cadastro/edição/exclusão.
        // DEPENDÊNCIAS: _context (EF Core) e ObterImagemUsuario.
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
        // Carrega a foto do usuário de forma segura, evitando travar o arquivo.
        // O QUE FAZ: se não houver caminho/arquivo válido retorna a imagem padrão;
        // caso contrário, abre o arquivo em modo leitura e cria um Bitmap independente.
        // POR QUE EXISTE: ler o arquivo diretamente no grid manteria o arquivo travado
        // (lock) e quebraria a grade ao operar sobre a imagem aberta; a cópia em memória
        // evita esse problema e usa o padrão "dispose" corretamente.
        // QUEM CHAMA: CarregarUsuarios.
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

        // Handler do campo de busca: filtra a lista conforme o texto digitado.
        // O QUE FAZ: a cada alteração no txtBusca, recarrega os usuários aplicando o
        // texto digitado como filtro de nome.
        // POR QUE EXISTE: oferece busca instantânea ao usuário (filtro "live"),
        // evitando um botão extra somente para pesquisar.
        // DEPENDÊNCIAS: chama CarregarUsuarios; registrado no construtor.
        private void TxtBusca_TextChanged(object? sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        // ====================== BOTÕES ======================
        // Abre o formulário de cadastro em modo "novo usuário".
        // O QUE FAZ: instancia frmCadastroUsuario sem registro existente, exibe em modo
        // modal (ShowDialog) e recarrega a lista ao fechar.
        // POR QUE EXISTE: concentra o fluxo "criar usuário" e garante que a grade reflita
        // imediatamente o novo registro cadastrado.
        // DEPENDÊNCIAS: frmCadastroUsuario e _context.
        private void btnNovo_Click(object sender, EventArgs e)
        {
            var frm = new frmCadastroUsuario(_context);
            frm.ShowDialog();
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        // Abre o formulário de cadastro em modo edição para o usuário selecionado.
        // O QUE FAZ: lê o Id da linha corrente na grade, localiza o usuário no banco
        // (Find) e, se existir, abre frmCadastroUsuario já com o registro carregado.
        // POR QUE EXISTE: permite alterar nome, senha, permissão e foto de um usuário
        // existente sem criar um registro duplicado.
        // DEPENDÊNCIAS: _context (EF Core) e frmCadastroUsuario.
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

        // Desativa (exclusão lógica) o usuário selecionado com confirmação prévia.
        // O QUE FAZ: valida a seleção, pede confirmação ao usuário, busca o registro
        // de forma assíncrona e marca-o como inativo (SetAtivo(false)).
        // POR QUE EXISTE: nunca apaga o registro do banco — a exclusão lógica preserva
        // o histórico e a auditoria; o login ignora usuários inativos.
        // DEPENDÊNCIAS: _context (FindAsync/SaveChangesAsync) e Serilog (Log).
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

        // Handler do botão "Atualizar": recarrega a listagem.
        // O QUE FAZ: simplesmente reexecuta CarregarUsuarios mantendo o filtro atual,
        // sincronizando a grade com o estado mais recente do banco.
        // POR QUE EXISTE: conveniência de UX para "refresh" manual sem depender de outra
        // operação que dispare o recarregamento.
        // DEPENDÊNCIAS: chama CarregarUsuarios; registrado no Designer (btnAtualizar).
        private void btnAtualizar_Click(object sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }

        // Abre a edição do usuário ao dar dois cliques na linha da grade.
        // O QUE FAZ: se o duplo clique ocorrer sobre uma linha válida, aciona
        // programaticamente o botão "Editar" (btnEditar.PerformClick).
        // POR QUE EXISTE: melhora a usabilidade — o usuário espera que um duplo clique
        // na linha abra o cadastro, sem precisar do botão.
        // DEPENDÊNCIAS: btnEditar.Click; registrado no Designer (dgvUsuarios).
        private void dgvUsuarios_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                btnEditar.PerformClick();
        }

        // Event handler required by Designer. If not used, keep empty to satisfy wiring.
        // Handler vazio exigido pelo Designer para o evento CellContentClick da grade.
        // O QUE FAZ: nada (no-op), existe apenas para satisfazer a ligação do Designer.
        // POR QUE EXISTE: a programação do Designer referencia este método; sem ele a
        // construção do formulário quebraria, mesmo que o clique no conteúdo da célula
        // não execute nenhuma lógica nesta tela.
        // QUEM CHAMA: registrado no Designer (dgvUsuarios.CellContentClick).
        private void dgvUsuarios_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // No-op
        }

        // Designer wiring expects this handler. Forward to the existing implementation.
        // Ponte entre o handler nomeado síncrono e a implementação assíncrona real.
        // O QUE FAZ: repassa a chamada para btnExcluir_Click (que é async void e já
        // contempla confirmação, desativação e log).
        // POR QUE EXISTE: o Designer faz a ligação do botão "Excluir" ao evento
        // btnExcluir_ClickAsync, então este método precisa existir para manter o wiring.
        // QUEM CHAMA: registrado no Designer (btnExcluir.Click).
        private void btnExcluir_ClickAsync(object sender, EventArgs e)
        {
            // Forward to existing async handler
            btnExcluir_Click(sender, e);
        }

        // Designer wiring expects this handler for search button.
        // Handler do botão "Buscar": aplica o filtro digitado na listagem.
        // O QUE FAZ: recarrega os usuários respeitando o texto atual de busca.
        // POR QUE EXISTE: o Designer liga o botão e o campo ao mesmo resultado; este
        // método atende ao botão explicitamente para quem prefere pesquisar só ao clicar.
        // DEPENDÊNCIAS: chama CarregarUsuarios; registrado no Designer (btnBuscarUsuario).
        private void btnBuscarUsuario_Click(object sender, EventArgs e)
        {
            CarregarUsuarios(txtBusca.Text.Trim());
        }
    }
}