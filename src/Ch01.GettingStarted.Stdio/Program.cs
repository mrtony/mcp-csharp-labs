using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

// stdio server 的 stdout 保留給 MCP 協定，log 一律導向 stderr
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public class FirstTools(ILogger<FirstTools> logger)
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool(Name = "fibonacci"), Description("Gets the first N Fibonacci numbers.")]
    public Dictionary<int, int> GetFibonacciNumbers(int count)
    {
        logger.LogInformation("Calculating Fibonacci numbers up to count: {Count}", count);

        var dict = new Dictionary<int, int>();
        if (count <= 0)
            return dict;

        int a = 0, b = 1;
        dict[1] = a; // First number (index 1) is 0

        if (count == 1)
            return dict;

        dict[2] = b; // Second number (index 2) is 1

        for (int i = 3; i <= count; i++)
        {
            int next = a + b;
            dict[i] = next;
            a = b;
            b = next;
        }

        return dict;
    }
}