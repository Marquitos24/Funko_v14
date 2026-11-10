namespace Funkos.Infrastructure.RateLimit;

public static class RateLimitConfig
{
    public const int GeneralLimit = 100;                                  // 100 por 15s
    public static readonly TimeSpan GeneralWindow = TimeSpan.FromSeconds(15);
    public const int AuthLimit = 10;                                      // 10/min en auth
    public const int WriteLimit = 20;                                     // 20/min en escritura
    public static readonly TimeSpan MinuteWindow = TimeSpan.FromMinutes(1);
    public const string AuthPathPrefix = "/api/auth/";

}