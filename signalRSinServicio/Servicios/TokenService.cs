using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace signalRSinServicio.Servicios;
public class TokenService
{
    private const string SecretKey = "helouda123"; // Debe ser una clave segura
    private readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

    public string GenerateJwtToken(string userId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("yourSecretKey"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, userId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var expiration = CalculateExpiration(); // Método para calcular la expiración dinámica

        var token = new JwtSecurityToken(
            issuer: "yourIssuer",
            audience: "yourAudience",
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private DateTime CalculateExpiration()
    {
        var now = DateTime.Now;
        if (now.DayOfWeek == DayOfWeek.Friday)
        {
            return now.AddDays(3); // Expiración de 3 días para los viernes
        }
        else if (now.DayOfWeek == DayOfWeek.Monday)
        {
            return now.AddDays(1.5); // Expiración de 1.5 días para los lunes
        }
        else
        {
            return now.AddDays(1); // Expiración estándar de 1 día
        }
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = false,
            ValidateAudience = false
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
