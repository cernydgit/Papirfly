using System.Text.Json;
using System.Text.Json.Serialization;
using PapirFly.Api;
using PapirFly.Application;
using PapirFly.Application.DTOs;
using PapirFly.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext(), preserveStaticLogger: true);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PapirFly.Api.xml"), includeControllerXmlComments: true);
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{typeof(ArticleResponse).Assembly.GetName().Name}.xml"));
});

var app = builder.Build();
await app.Services.InitializeStorageAsync();
app.UseSerilogRequestLogging(options => options.Logger = app.Services.GetRequiredService<Serilog.ILogger>());
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();

/// <summary>Exposes the application entry point to the integration test host.</summary>
public partial class Program;
