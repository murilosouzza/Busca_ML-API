using BuscaML.Api.Data;
using BuscaML.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

var mlBaseUrl = builder.Configuration["MercadoLivre:BaseUrl"] ?? "https://api.mercadolibre.com";

// Usa o cliente fake quando MercadoLivre:UseFake=true (default: true em Development se não houver
// credencial configurada). Assim o projeto roda de ponta a ponta sem token nem rede.
var temCredencial =
    !string.IsNullOrWhiteSpace(builder.Configuration["MercadoLivre:AccessToken"]) ||
    !string.IsNullOrWhiteSpace(builder.Configuration["MercadoLivre:ClientId"]);
var usarFake = builder.Configuration.GetValue<bool?>("MercadoLivre:UseFake")
    ?? (builder.Environment.IsDevelopment() && !temCredencial);

if (usarFake)
{
    builder.Services.AddSingleton<IMercadoLivreClient, FakeMercadoLivreClient>();
}
else
{
    // Client isolado para o /oauth/token (não passa pelo AuthHandler, senão dava recursão).
    builder.Services.AddHttpClient("ml-oauth", client => client.BaseAddress = new Uri(mlBaseUrl));
    builder.Services.AddTransient<MercadoLivreAuthHandler>();

    builder.Services.AddHttpClient<IMercadoLivreClient, MercadoLivreClient>(client =>
        {
            client.BaseAddress = new Uri(mlBaseUrl);
        })
        .AddHttpMessageHandler<MercadoLivreAuthHandler>();
}

builder.Services.AddScoped<IBuscaService, BuscaService>();
builder.Services.AddHostedService<HighlightsBackgroundService>();

var app = builder.Build();

// Cria o banco a partir do modelo se ele ainda não existir (dev).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
