using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharpA2A.AspNetCore;
using SharpA2A.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("A2AAgentServer");
    })
    .WithTracing(tracing => tracing
        .AddSource(SharpA2A.Core.TaskManager.ActivitySource.Name)
        .AddSource(A2AProcessor.ActivitySource.Name)
        .AddSource(HostedClientAgent.ActivitySource.Name)
        .AddSource(ResearcherAgent.ActivitySource.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
        })
        );


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var echoAgent = new EchoAgent();
var echoTaskManager = new TaskManager();
echoAgent.Attach(echoTaskManager);
app.MapA2A(echoTaskManager, "/echo");
app.MapHttpA2A(echoTaskManager, "/echo");

var hostedClientAgent = new HostedClientAgent();
var hostedClientTaskManager = new TaskManager();
hostedClientAgent.Attach(hostedClientTaskManager);
app.MapA2A(hostedClientTaskManager, "/hostedclient");

var researcherAgent = new ResearcherAgent();
var researcherTaskManager = new TaskManager();
researcherAgent.Attach(researcherTaskManager);
app.MapA2A(researcherTaskManager, "/researcher");

app.Run();

