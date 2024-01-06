using System;
using System.Runtime.Versioning;
using Fragile;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharpLab.Container.Manager.Azure;
using SharpLab.Container.Manager.Endpoints;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager;

[SupportedOSPlatform("windows")]
public class Startup {
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

    public void ConfigureServices(IServiceCollection services)
    {
        // TODO: proper DI, e.g. Autofac
        services.AddSingleton(new ProcessRunnerConfiguration(
            workingDirectoryPath: AppContext.BaseDirectory,
            exeFileName: Container.Program.ExeFileName,
            essentialAccessCapabilitySid: "S-1-15-3-1024-4233803318-1181731508-1220533431-3050556506-2713139869-1168708946-594703785-1824610955",
            maximumMemorySize: 30 * 1024 * 1024,
            maximumCpuPercentage: 1
        ));
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<StatusEndpoint>();

        var authorizationToken = Environment.GetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_AUTHORIZATION_TOKEN")
            ?? throw new Exception("Required environment variable SHARPLAB_CONTAINER_HOST_AUTHORIZATION_TOKEN was not provided.");
        services.AddSingleton(new ExecutionEndpointSettings(authorizationToken));
        services.AddSingleton<ExecutionEndpoint>();

        services.AddSingleton<ContainerPool>();

        services.AddHostedService<ContainerAllocationWorker>();
        services.AddSingleton<ContainerCleanupWorker>();
        services.AddHostedService(c => c.GetRequiredService<ContainerCleanupWorker>());

        services.AddSingleton<StdinWriter>();
        services.AddSingleton<StdoutReader>();
        services.AddSingleton<ExecutionProcessor>();
        services.AddSingleton<CrashSuspensionManager>();
        services.AddSingleton<ExecutionManager>();

        ConfigureAzureDependentServices(services);
    }

    private void ConfigureAzureDependentServices(IServiceCollection services) {
        var connectionString = Environment.GetEnvironmentVariable("SHARPLAB_TELEMETRY_CONNECTION_STRING");
        if (connectionString == null) {
            Console.WriteLine("[WARN] AppInsights connection string was not found.");
            return;
        }

        var configuration = new TelemetryConfiguration { ConnectionString = connectionString };
        services.AddSingleton(new TelemetryClient(configuration));
        services.AddHostedService<ContainerCountMetricReporter>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app) {
        app.UseRouting();

        app.UseEndpoints(endpoints => {
            endpoints.MapGet("/status", app.ApplicationServices.GetRequiredService<StatusEndpoint>().ExecuteAsync);
            endpoints.MapPost("/", app.ApplicationServices.GetRequiredService<ExecutionEndpoint>().ExecuteAsync);
        });
    }
}
