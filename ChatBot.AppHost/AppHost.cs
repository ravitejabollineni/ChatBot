var builder = DistributedApplication.CreateBuilder(args);

// ------------------------------------------------------------
// PostgreSQL credentials
// ------------------------------------------------------------

var postgresPassword = builder.AddParameter(
    "postgres-password",
    secret: true);

// ------------------------------------------------------------
// PostgreSQL
// ------------------------------------------------------------

var postgres = builder
    .AddPostgres(
        "postgres",
        password: postgresPassword)
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg18-trixie")
    .WithHostPort(5432)
    .WithDataVolume();

// Aspire picks the data mount path from a parseable image tag; "pg18" is not, so it falls back
// to /var/lib/postgresql/data. PostgreSQL 18+ images require the mount at /var/lib/postgresql.
var dataMount = postgres.Resource.Annotations
    .OfType<ContainerMountAnnotation>()
    .Single(m => m.Target == "/var/lib/postgresql/data");
postgres.Resource.Annotations.Remove(dataMount);
postgres.WithVolume(dataMount.Source!, "/var/lib/postgresql");

var database = postgres.AddDatabase("chatbot");

// ------------------------------------------------------------
// Liquibase
// ------------------------------------------------------------

var liquibase = builder
    .AddDockerfile(
        "liquibase",
        "../ChatBot.ApiService/Infrastructure/Database/Liquibase")
    .WithArgs("update")
    .WithEnvironment(
        "LIQUIBASE_COMMAND_URL",
        "jdbc:postgresql://postgres:5432/chatbot")
    .WithEnvironment(
        "LIQUIBASE_COMMAND_USERNAME",
        "postgres")
    .WithEnvironment(
        "LIQUIBASE_COMMAND_PASSWORD",
        postgresPassword)
    .WaitFor(database);

// ------------------------------------------------------------
// API
// ------------------------------------------------------------

var apiService = builder
    .AddProject<Projects.ChatBot_Api>("api")
    .WithReference(database)
    .WithHttpHealthCheck("/health")
    .WaitFor(database);

// ------------------------------------------------------------
// Web
// ------------------------------------------------------------

builder
    .AddProject<Projects.ChatBot_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// ------------------------------------------------------------

builder.Build().Run();