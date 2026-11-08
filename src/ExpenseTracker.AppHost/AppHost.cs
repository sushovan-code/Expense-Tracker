// Local development orchestrator: runs the API and the React client together and collects
// their logs, traces and metrics (including browser telemetry) in the Aspire dashboard.
//
// Development only by design. Staging and Production run the API and client on their own,
// using their configs/ settings (see README → Environments & configuration).

var builder = DistributedApplication.CreateBuilder(args);

if (builder.ExecutionContext.IsPublishMode)
{
    throw new NotSupportedException(
        "The ExpenseTracker AppHost is for local development only. " +
        "Deploy the API and client with their Staging/Production configuration instead.");
}

// The API's "http" launch profile → ASPNETCORE_ENVIRONMENT=Development, http://localhost:7010.
var api = builder.AddProject<Projects.ExpenseTracker_Api>("api", launchProfileName: "http")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar API reference" });

// `npm run dev` → Vite in "development" mode (client/configs/.env.development), http://localhost:5010.
builder.AddViteApp("client", "../web/expense-tracker-web")
    .WithEndpoint("http", endpoint => endpoint.Port = 5010)
    // Overrides API_PROXY_TARGET from .env.development with the API's actual endpoint.
    .WithEnvironment("API_PROXY_TARGET", api.GetEndpoint("http"))
    // Browsers can only send OTLP over HTTP, so give the client the dashboard's HTTP endpoint.
    .WithOtlpExporter(OtlpProtocol.HttpProtobuf)
    .WaitFor(api);

builder.Build().Run();
