using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernelAgent;
using SharpA2A.AspNetCore;
using SharpA2A.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient().AddLogging();
var app = builder.Build();

var configuration = app.Configuration;
var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
var logger = app.Logger;

var agent = new SemanticKernelTravelAgent(configuration, httpClient, logger);
var taskManager = new TaskManager();
agent.Attach(taskManager);
app.MapA2A(taskManager, string.Empty);

await app.RunAsync();
