using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using TimescaleEdgeIoT.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<DataIngestionWorker>();

var host = builder.Build();
host.Run();

class DataIngestionWorker : BackgroundService
{
    private readonly ILogger<DataIngestionWorker> _logger;
    private readonly IConfiguration _configuration;

    public DataIngestionWorker(ILogger<DataIngestionWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration.GetConnectionString("iotdb");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Database connection string 'iotdb' not found");
            return;
        }

        _logger.LogInformation("Starting Data Ingestion Worker...");
        _logger.LogInformation("Connection string: {ConnectionString}", connectionString);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(stoppingToken);

        // Create table and hypertable if not exists
        await InitializeDatabaseAsync(conn, stoppingToken);

        // Simulate IoT sensor data ingestion
        var random = new Random();
        var deviceIds = new[] { "sensor-001", "sensor-002", "sensor-003", "sensor-004" };

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var deviceId in deviceIds)
            {
                var temperature = 15.0 + random.NextDouble() * 20.0; // 15-35°C
                var humidity = 30.0 + random.NextDouble() * 40.0;    // 30-70%
                var pressure = 980.0 + random.NextDouble() * 40.0;   // 980-1020 hPa

                await InsertSensorDataAsync(conn, deviceId, temperature, humidity, pressure, stoppingToken);
                
                _logger.LogInformation("Inserted data - Device: {DeviceId}, Temp: {Temperature:F2}°C, Humidity: {Humidity:F2}%, Pressure: {Pressure:F2} hPa",
                    deviceId, temperature, humidity, pressure);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task InitializeDatabaseAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        // Enable TimescaleDB extension
        var enableExtensionSql = "CREATE EXTENSION IF NOT EXISTS timescaledb;";
        await using (var cmd = new NpgsqlCommand(enableExtensionSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("TimescaleDB extension enabled");
        }

        // Create table
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS sensor_data (
                time TIMESTAMPTZ NOT NULL,
                device_id TEXT NOT NULL,
                temperature DOUBLE PRECISION,
                humidity DOUBLE PRECISION,
                pressure DOUBLE PRECISION
            );";
        
        await using (var cmd = new NpgsqlCommand(createTableSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("sensor_data table created or already exists");
        }

        // Convert to hypertable
        var createHypertableSql = @"
            SELECT create_hypertable('sensor_data', 'time', if_not_exists => TRUE);";
        
        await using (var cmd = new NpgsqlCommand(createHypertableSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Hypertable created or already exists");
        }
    }

    private async Task InsertSensorDataAsync(
        NpgsqlConnection conn, 
        string deviceId, 
        double temperature, 
        double humidity, 
        double pressure,
        CancellationToken cancellationToken)
    {
        var sql = @"
            INSERT INTO sensor_data (time, device_id, temperature, humidity, pressure)
            VALUES (@time, @deviceId, @temperature, @humidity, @pressure);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("time", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("deviceId", deviceId);
        cmd.Parameters.AddWithValue("temperature", temperature);
        cmd.Parameters.AddWithValue("humidity", humidity);
        cmd.Parameters.AddWithValue("pressure", pressure);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
