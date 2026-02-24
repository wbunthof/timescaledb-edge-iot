using Npgsql;
using TimescaleEdgeIoT.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// API endpoint to get sensor data for graphing
app.MapGet("/api/sensor-data", async (string? deviceId, int hours = 1) =>
{
    var connectionString = builder.Configuration.GetConnectionString("iotdb");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.Problem("Database connection not configured");
    }

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    var sql = deviceId != null
        ? @"SELECT time, device_id, temperature, humidity, pressure 
            FROM sensor_data 
            WHERE device_id = @deviceId AND time > NOW() - INTERVAL '@hours hours'
            ORDER BY time DESC LIMIT 1000"
        : @"SELECT time, device_id, temperature, humidity, pressure 
            FROM sensor_data 
            WHERE time > NOW() - INTERVAL '@hours hours'
            ORDER BY time DESC LIMIT 1000";

    await using var cmd = new NpgsqlCommand(sql.Replace("@hours", hours.ToString()), conn);
    if (deviceId != null)
    {
        cmd.Parameters.AddWithValue("deviceId", deviceId);
    }

    var data = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        data.Add(new
        {
            time = reader.GetDateTime(0),
            deviceId = reader.GetString(1),
            temperature = reader.GetDouble(2),
            humidity = reader.GetDouble(3),
            pressure = reader.GetDouble(4)
        });
    }

    return Results.Ok(data);
});

app.Run();
