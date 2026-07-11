var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Hello_AspNetCoreMcp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var mcp = builder.AddProject<Projects.Hello_AspNetCoreMcp_McpServer>("mcp-server")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService);

builder.AddMcpInspector("mcp-inspector")
    .WithMcpServer(mcp, path: "");

builder.Build().Run();
