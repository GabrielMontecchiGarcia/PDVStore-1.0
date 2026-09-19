using System;
using System.Data.Common;
using System.Linq;
namespace PDVStore.Helpers
{
    public static class ConnectionHelper
    {
        // Retorna a connection string padrão do sistema, apontando para o SQL Server
        // LocalDB local (instância "(localdb)\MSSQLLocalDB") e o banco
        // PDV_StoreDB com autenticação do Windows. É a fonte única de conexão
        // usada pelos serviços (ex.: VerificacaoSistemaService valida que ela foi
        // carregada). O comentário interno avisa: troque a instância se for usar
        // SQL Server/Express real. Não lança exceções — retorna a string fixa.
        public static string GetConnectionString()
        {
            // Default localdb instance - change if using SQL Server/Express
            return @"Server=(localdb)\MSSQLLocalDB;Database=PDV_StoreDB;Trusted_Connection=True;";
        }

        // Retorna a connection string decomposta em linhas legíveis (chave: valor),
        // usando DbConnectionStringBuilder para interpretar os componentes.
        // Depende de GetConnectionString. É um recurso de diagnóstico/exibição
        // (ex.: tela "Sobre" ou verificação do sistema). Se a string for inválida
        // para o parser, captura a exceção e devolve a string bruta com aviso —
        // nunca quebra o chamador.
        public static string GetReadableConnectionStringInfo()
        {
            var cs = GetConnectionString();
            try
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = cs };
                return string.Join(Environment.NewLine,
                    builder.Keys.Cast<string>().Select(k => $"{k}: {builder[k]}"));
            }
            catch
            {
                return "Invalid connection string: " + cs;
            }
        }
    }
}
