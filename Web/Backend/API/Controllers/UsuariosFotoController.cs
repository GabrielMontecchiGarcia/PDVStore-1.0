using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDVStore.DTOs;

namespace PDVStore.API.Controllers;

/// <summary>
/// Upload da foto do usuário — equivalente ao recurso "Escolher Foto" do
/// frmCadastroUsuario, que copia a imagem para a pasta Resources/Users.
///
/// Segurança aplicada:
/// - somente Administrador (gestão de usuários);
/// - tamanho máximo de 2 MB e extensões permitidas (jpg/jpeg/png/bmp);
/// - validação dos bytes mágicos do arquivo (a extensão não é confiável);
/// - nome do arquivo gerado pelo servidor (evita path traversal e colisão);
/// - gravação em wwwroot/uploads/usuarios, servida como arquivo estático.
/// O caminho retornado é o que deve ser gravado em UsuarioCaixa.FotoPath.
/// </summary>
[ApiController]
[Route("api/usuarios/foto")]
[Authorize(Policy = "Administrador")]
public class UsuariosFotoController : ControllerBase
{
    private const long TamanhoMaximoBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png", ".bmp" };

    private readonly IWebHostEnvironment _ambiente;

    public UsuariosFotoController(IWebHostEnvironment ambiente) => _ambiente = ambiente;

    /// <summary>Recebe a imagem e devolve o caminho público para gravar no cadastro.</summary>
    [HttpPost]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Enviar(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "Selecione uma imagem para enviar.",
                TraceId = HttpContext.TraceIdentifier
            });

        if (arquivo.Length > TamanhoMaximoBytes)
            return BadRequest(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "A imagem deve ter no máximo 2 MB.",
                TraceId = HttpContext.TraceIdentifier
            });

        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesPermitidas.Contains(extensao))
            return BadRequest(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "Formato inválido. Envie uma imagem JPG, PNG ou BMP.",
                TraceId = HttpContext.TraceIdentifier
            });

        var pasta = Path.Combine(
            _ambiente.WebRootPath ?? Path.Combine(_ambiente.ContentRootPath, "wwwroot"),
            "uploads", "usuarios");

        Directory.CreateDirectory(pasta);

        // Nome gerado no servidor: o nome enviado pelo cliente nunca é usado.
        var nomeArquivo = $"user_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}"[..40] + extensao;
        var caminho = Path.Combine(pasta, nomeArquivo);

        // Valida a assinatura do arquivo (magic bytes) ANTES de gravar, lendo do
        // arquivo enviado (e não do destino, que ainda está vazio).
        await using var origem = arquivo.OpenReadStream();
        var cabecalho = new byte[12];
        var lidos = await origem.ReadAsync(cabecalho, ct);

        if (lidos < 4 || !EhImagem(cabecalho, lidos))
        {
            return BadRequest(new ErroResposta
            {
                Status = StatusCodes.Status400BadRequest,
                Mensagem = "O arquivo enviado não é uma imagem válida.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        origem.Position = 0;

        await using (var destino = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await origem.CopyToAsync(destino, ct);
        }

        return Ok(new { caminho = $"/uploads/usuarios/{nomeArquivo}" });
    }

    /// <summary>Confere os bytes mágicos de JPEG, PNG ou BMP.</summary>
    private static bool EhImagem(byte[] dados, int lidos)
    {
        // JPEG: FF D8 FF
        if (lidos >= 3 && dados[0] == 0xFF && dados[1] == 0xD8 && dados[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (lidos >= 8 && dados[0] == 0x89 && dados[1] == 0x50 && dados[2] == 0x4E && dados[3] == 0x47)
            return true;

        // BMP: 42 4D
        if (lidos >= 2 && dados[0] == 0x42 && dados[1] == 0x4D)
            return true;

        return false;
    }
}
