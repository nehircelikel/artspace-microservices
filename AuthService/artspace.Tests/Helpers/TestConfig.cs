using Microsoft.Extensions.Configuration;

namespace artspace.Tests.Helpers;

/// <summary>JWT settings used by both the unit tests (AuthService) and the test host.</summary>
public static class TestConfig
{
    public const string Secret = "test-secret-key-for-integration-tests-only-1234567890";
    public const string Issuer = "artspace-test";
    public const string Audience = "artspace-test";

    public static readonly Dictionary<string, string?> Values = new()
    {
        ["Jwt:Secret"] = Secret,
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
    };

    public static IConfiguration Build() =>
        new ConfigurationBuilder().AddInMemoryCollection(Values).Build();
}
