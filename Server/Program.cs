using TradingAgent.AgentRuntime.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgentRuntime();

var app = builder.Build();
app.MapGet("/", () => "Hello World!");
app.Run();