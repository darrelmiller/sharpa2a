using A2ALib;
using A2ATransport;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var researcherAgent = new ResearcherAgent();

var researcherTaskManager = new TaskManager();
researcherAgent.Attach(researcherTaskManager);
app.MapA2A(researcherTaskManager,"/researcher");

var echoAgent = new EchoAgent();
var echoTaskManager = new TaskManager();
echoAgent.Attach(echoTaskManager);
app.MapA2A(echoTaskManager,"/echo");

var hostedClientAgent = new HostedClientAgent();
var hostedClientTaskManager = new TaskManager();
hostedClientAgent.Attach(hostedClientTaskManager);
app.MapA2A(hostedClientTaskManager,"/hostedclient");

app.Run();

