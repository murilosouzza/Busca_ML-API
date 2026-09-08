using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BuscaML.Api.Services;

/// <summary>
/// Injeta o header Authorization: Bearer em toda chamada ao Mercado Livre.
///
/// Ordem de resolução do token:
/// 1. MercadoLivre:AccessToken fixo no appsettings/user-secrets (mais simples: cola um token e pronto);
/// 2. client_credentials com MercadoLivre:ClientId + MercadoLivre:ClientSecret (renova sozinho);
/// 3. nenhum dos dois -> não adiciona header (deixa a chamada seguir e provavelmente tomar 401/403).
/// </summary>
public class MercadoLivreAuthHandler : DelegatingHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MercadoLivreAuthHandler> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _tokenEmCache;
    private DateTimeOffset _expiraEm = DateTimeOffset.MinValue;
    private bool _jaAvisouSemCredencial;

    public MercadoLivreAuthHandler(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<MercadoLivreAuthHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await ObterTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> ObterTokenAsync(CancellationToken ct)
    {
        var tokenFixo = _config["MercadoLivre:AccessToken"];
        if (!string.IsNullOrWhiteSpace(tokenFixo))
            return tokenFixo;

        var clientId = _config["MercadoLivre:ClientId"];
        var clientSecret = _config["MercadoLivre:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            if (!_jaAvisouSemCredencial)
            {
                _logger.LogWarning(
                    "Sem MercadoLivre:AccessToken e sem ClientId/ClientSecret. " +
                    "As chamadas ao Mercado Livre vão sair sem Authorization.");
                _jaAvisouSemCredencial = true;
            }
            return null;
        }

        if (_tokenEmCache is not null && DateTimeOffset.UtcNow < _expiraEm)
            return _tokenEmCache;

        await _gate.WaitAsync(ct);
        try
        {
            if (_tokenEmCache is not null && DateTimeOffset.UtcNow < _expiraEm)
                return _tokenEmCache;

            var http = _httpClientFactory.CreateClient("ml-oauth");

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });

            using var resposta = await http.PostAsync("/oauth/token", form, ct);

            if (!resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Falha ao obter token do Mercado Livre ({Status}): {Corpo}",
                    (int)resposta.StatusCode, corpo);
                return null;
            }

            var dados = await resposta.Content.ReadFromJsonAsync<OAuthTokenResponse>(ct);
            if (dados is null || string.IsNullOrWhiteSpace(dados.AccessToken))
            {
                _logger.LogError("Resposta de token do Mercado Livre veio vazia ou sem access_token.");
                return null;
            }

            _tokenEmCache = dados.AccessToken;
            // Renova 60s antes de expirar de fato, pra evitar corrida com o relógio da ML.
            _expiraEm = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, dados.ExpiresIn - 60));

            _logger.LogInformation("Novo token do Mercado Livre obtido; válido por ~{Segundos}s.", dados.ExpiresIn);
            return _tokenEmCache;
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
