var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ChatBot_Api>("api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.ChatBot_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
