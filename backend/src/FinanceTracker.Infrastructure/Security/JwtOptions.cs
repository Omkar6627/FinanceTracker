namespace FinanceTracker.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "FinanceTracker";
    public string Audience { get; set; } = "FinanceTracker.Clients";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}
