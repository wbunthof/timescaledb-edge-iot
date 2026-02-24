var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL/TimescaleDB
var postgres = builder.AddPostgres("postgres")
    .WithImage("timescale/timescaledb", "latest-pg17")
    .WithEnvironment("POSTGRES_DB", "iot")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("iotdb");

// Add Data Ingestion Console App
var dataIngestion = builder.AddProject<Projects.TimescaleEdgeIoT_DataIngestion>("dataingestion")
    .WithReference(db)
    .WaitFor(db);

// Add Web App for Data Visualization
var webApp = builder.AddProject<Projects.TimescaleEdgeIoT_Web>("webapp")
    .WithReference(db)
    .WithExternalHttpEndpoints()
    .WaitFor(db);

builder.Build().Run();
