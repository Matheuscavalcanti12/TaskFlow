using Rotas;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("cadastro", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
});

var key = Encoding.UTF8.GetBytes("minha_chave_super_secreta_3112_123456789123456789");
//fala pro sistema o tipo de autenticação
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //permite que o uso da rota sem https
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        //regras de validação 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = "meuSistema",
            ValidAudience = "meuSistema",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
var app = builder.Build();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.cadastro();
app.login();
app.perfil();
app.cadastroAtividade();
app.buscarAtividade();
app.atualizarAtividade();
app.removerAtividade();
app.Categorias();
app.Run();
