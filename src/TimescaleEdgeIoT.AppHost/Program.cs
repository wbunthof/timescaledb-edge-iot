var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL/TimescaleDB
var postgres = builder.AddPostgres("postgres")
    .WithImage("timescale/timescaledb", "latest-pg17")
    .WithEnvironment("POSTGRES_DB", "iot")
    .WithPgAdmin();

var db = postgres.AddDatabase("iotdb");

builder.Build().Run();
