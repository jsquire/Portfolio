using Microsoft.Extensions.Configuration;

namespace Squire.NumTic.Tests;

/// <summary>
///   Represents the ambient environment for test execution, providing
///   access to configuration values from multiple sources.
/// </summary>
///
public static class TestEnvironment
{
    /// <summary>The configuration root containing all configuration sources.</summary>
    private static readonly IConfigurationRoot Configuration;

    /// <summary>
    ///   Initializes static members of the <see cref="TestEnvironment"/> class.
    /// </summary>
    ///
    static TestEnvironment()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(typeof(TestEnvironment).Assembly, optional: true)
            .AddEnvironmentVariables(prefix: "NumTic_");

        Configuration = builder.Build();
    }

    /// <summary>
    ///   Indicates whether live tests should run by default.
    /// </summary>
    ///
    /// <value>
    ///   <c>true</c> if live tests should run by default; otherwise, <c>false</c>.
    /// </value>
    ///
    public static bool RunLiveTestsByDefault =>
        bool.TryParse(Configuration["TestEnvironment:RunLiveTestsByDefault"], out var value) && value;

    /// <summary>
    ///   The Azure OpenAI endpoint URL for testing.
    /// </summary>
    ///
    public static Uri? AzureOpenAIEndpoint
    {
        get
        {
            var endpoint = Configuration["Azure:OpenAI:Endpoint"];
            return endpoint is { Length: 0 } ? null : new Uri(endpoint!);
        }
    }

    /// <summary>
    ///   The Azure OpenAI authorization scope for testing.
    /// </summary>
    ///
    public static string? AzureOpenAIAuthorizationScope =>
        Configuration["Azure:OpenAI:AuthorizationScope"];

    /// <summary>
    ///   The name of the model deployment to use as the OpenAI model for
    ///   client operations.
    /// </summary>
    ///
    public static string? AzureOpenAIModelName =>
        Configuration["Azure:OpenAI:ModelDeploymentName"];
}
