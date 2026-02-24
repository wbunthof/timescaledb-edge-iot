# timescaledb-edge-iot

This repository demonstrates the use of TimescaleDB as a database on the edge for storing and visualizing IoT time-series data using .NET 10 and Aspire.

## Architecture

This solution consists of:

- **.NET 10** - Latest .NET framework
- **Aspire** - Cloud-native application orchestration (using NuGet packages)
- **TimescaleDB** - PostgreSQL-based time-series database
- **Console App** - Data ingestion service that simulates IoT sensor data
- **ASP.NET Web App** - Web dashboard for visualizing sensor data with real-time charts

## Project Structure

```
src/
├── TimescaleEdgeIoT.AppHost/          # Aspire orchestrator (optional)
├── TimescaleEdgeIoT.DataIngestion/    # Console app for data ingestion
├── TimescaleEdgeIoT.Web/              # ASP.NET web app for visualization
└── TimescaleEdgeIoT.ServiceDefaults/  # Shared service configuration
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) and Docker Compose

## Quick Start

### Option 1: Using Docker Compose (Recommended for standalone usage)

1. **Start TimescaleDB using Docker Compose:**

   ```bash
   docker-compose up -d
   ```

   This starts:
   - TimescaleDB on port 5432
   - pgAdmin on port 5050 (http://localhost:5050)
     - Email: admin@admin.com
     - Password: admin

2. **Run the Data Ingestion Console App:**

   ```bash
   cd src/TimescaleEdgeIoT.DataIngestion
   dotnet run
   ```

   The app will:
   - Create the necessary database schema
   - Enable the TimescaleDB extension
   - Create a hypertable for sensor data
   - Start ingesting simulated sensor data every 5 seconds

3. **Run the Web Dashboard:**

   ```bash
   cd src/TimescaleEdgeIoT.Web
   dotnet run
   ```

   Open your browser to the URL shown in the console (typically http://localhost:5182 or https://localhost:7040)

### Option 2: Using Aspire AppHost (Orchestrated)

Run the Aspire AppHost to orchestrate all services:

```bash
cd src/TimescaleEdgeIoT.AppHost
dotnet run
```

This will start:
- TimescaleDB container
- pgAdmin container
- Aspire dashboard

> Note: You'll still need to run the DataIngestion and Web apps separately as project references aren't configured in this simplified AppHost.

## Features

### Data Ingestion

The console app simulates 4 IoT sensors (`sensor-001` through `sensor-004`) that continuously send:
- **Temperature** (15-35°C)
- **Humidity** (30-70%)
- **Pressure** (980-1020 hPa)

Data is inserted into TimescaleDB every 5 seconds.

### Web Dashboard

The web dashboard provides:
- Real-time charts using Chart.js
- Filter by device (individual sensor or all sensors)
- Time range selection (1 hour, 6 hours, 24 hours)
- Auto-refresh every 5 seconds
- Three separate graphs:
  - Temperature over time
  - Humidity over time
  - Pressure over time

## Database Schema

The `sensor_data` table is automatically created as a TimescaleDB hypertable:

```sql
CREATE TABLE sensor_data (
    time TIMESTAMPTZ NOT NULL,
    device_id TEXT NOT NULL,
    temperature DOUBLE PRECISION,
    humidity DOUBLE PRECISION,
    pressure DOUBLE PRECISION
);

SELECT create_hypertable('sensor_data', 'time');
```

## Configuration

### Connection Strings

When using Docker Compose, the default connection string is:

```
Host=localhost;Port=5432;Database=iot;Username=postgres;Password=postgres
```

This can be configured in `appsettings.json` or via environment variables.

### Environment Variables

For the Data Ingestion and Web apps, you can override the connection string using:

```bash
export ConnectionStrings__iotdb="Host=localhost;Port=5432;Database=iot;Username=postgres;Password=postgres"
```

## Development

### Building the Solution

```bash
cd src
dotnet build
```

### Running Tests

(No tests implemented yet)

### Accessing pgAdmin

1. Navigate to http://localhost:5050
2. Login with:
   - Email: admin@admin.com
   - Password: admin
3. Add a new server:
   - Host: timescaledb (or localhost if accessing from host machine)
   - Port: 5432
   - Username: postgres
   - Password: postgres
   - Database: iot

## TimescaleDB Features

This project leverages TimescaleDB's powerful time-series capabilities:

- **Hypertables**: Automatic partitioning of time-series data
- **Compression**: Efficient storage for historical data
- **Continuous Aggregates**: Pre-computed rollups for faster queries
- **Data Retention**: Automatic data lifecycle management

## Technologies Used

- **.NET 10** - Application framework
- **Aspire** - Cloud-native orchestration
- **TimescaleDB** - Time-series database (PostgreSQL extension)
- **Npgsql** - PostgreSQL driver for .NET
- **Chart.js** - JavaScript charting library
- **ASP.NET Core** - Web framework
- **Razor Pages** - Web UI framework

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

