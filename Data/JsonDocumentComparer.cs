using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuscaML.Api.Data;

/// <summary>
/// Faz o EF Core considerar um JsonDocument "alterado" só quando o conteúdo muda,
/// e não a cada nova instância criada com o mesmo texto.
/// </summary>
public static class JsonDocumentComparer
{
    public static ValueComparer<JsonDocument> Criar()
    {
        return new ValueComparer<JsonDocument>(
            (a, b) => a!.RootElement.GetRawText() == b!.RootElement.GetRawText(),
            doc => doc.RootElement.GetRawText().GetHashCode(),
            doc => JsonDocument.Parse(doc.RootElement.GetRawText(), default));
    }
}
