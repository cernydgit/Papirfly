using System.Text.Json;
using System.Text.Json.Serialization;
using PapirFly.Api;
using PapirFly.Application;
using PapirFly.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
// Swagger uses MVC's serializer settings, while Minimal APIs use HTTP JSON settings.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => ConfigureJson(options.JsonSerializerOptions));
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.MapArticleEndpoints();
app.Run();

static void ConfigureJson(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.NumberHandling = JsonNumberHandling.Strict;
    options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
}

public partial class Program;
