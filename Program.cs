using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    openTelemetryBuilder.UseAzureMonitorExporter(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

builder.Services.AddSingleton(_ =>
{
    var connectionString = Environment.GetEnvironmentVariable("CosmosDbConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("CosmosDbConnection is required for experience-service.");
    }

    return new CosmosClient(connectionString);
}).Configure<WorkerOptions>(options =>
{
    options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });
});

builder.Build().Run();
