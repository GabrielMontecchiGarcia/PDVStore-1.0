using Xunit;

// A execução é sequencial: os testes de integração usam variáveis de ambiente
// para configurar a API em memória, exigindo isolamento entre as classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
