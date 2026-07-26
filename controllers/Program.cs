using Rotas;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

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
app.UseAuthentication();
app.UseAuthorization();
app.cadastro();
app.Run();
