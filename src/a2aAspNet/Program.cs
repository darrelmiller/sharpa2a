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
TaskManager? taskManager = new(new HttpClient(), new DemoAgent());

app.MapA2A(taskManager);

app.Run();

