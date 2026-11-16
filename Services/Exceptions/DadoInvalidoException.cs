namespace BuscaML.Api.Services.Exceptions;

public class DadoInvalidoException : Exception
{
    public DadoInvalidoException(string mensagem) : base(mensagem) { }
}
