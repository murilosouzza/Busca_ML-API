namespace BuscaML.Api.Services.Exceptions;

public class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string mensagem) : base(mensagem) { }
}
