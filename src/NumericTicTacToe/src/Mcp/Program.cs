using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Squire.NumTic;
using Squire.NumTic.Contracts;
using Squire.NumTic.Mcp;

// Create the host and register it as an MCP server
// with stdio transport.

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton<IGameInterface, McpBotPlayerGameInterface>()
    .AddSingleton(_ => new McpRenderer(GameState.CreateDefault()));


builder.Services
    .AddMemoryCache()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<NumericTicTacToeGameTools>();

// Configure logging and redirect all levels to stderr, as
// the MCP protocol uses stdout for messages.

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

await builder.Build().RunAsync();
