using System.Text.Json;
using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace LANRescue.Infrastructure.Persistence;

/// <summary>
/// Stores each complete ManagedDevice aggregate as JSON while keeping frequently queried
/// identity/status columns indexed in SQLite. The database is local to the current Windows user.
/// </summary>
public sealed class SqliteDeviceRepository : IDeviceRepository
{
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        IgnoreReadOnlyProperties = true
    };

    public SqliteDeviceRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A database path is required.", nameof(databasePath));

        DatabasePath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(DatabasePath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("The database path must include a directory.", nameof(databasePath));

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public string DatabasePath { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS Devices (
                Id TEXT NOT NULL PRIMARY KEY,
                DeviceCode TEXT NOT NULL COLLATE NOCASE,
                DeviceName TEXT NOT NULL,
                AgentId TEXT NULL,
                IsManaged INTEGER NOT NULL DEFAULT 1,
                UpdatedAt TEXT NOT NULL,
                PayloadJson TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Devices_DeviceCode
                ON Devices(DeviceCode COLLATE NOCASE);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Devices_AgentId
                ON Devices(AgentId) WHERE AgentId IS NOT NULL;
            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagedDevice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PayloadJson
            FROM Devices
            ORDER BY IsManaged DESC, DeviceCode COLLATE NOCASE;
            """;

        var result = new List<ManagedDevice>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var json = reader.GetString(0);
            var device = JsonSerializer.Deserialize<ManagedDevice>(json, _jsonOptions);
            if (device is not null)
                result.Add(device);
        }

        return result;
    }

    public async Task<ManagedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT PayloadJson FROM Devices WHERE Id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return json is null ? null : JsonSerializer.Deserialize<ManagedDevice>(json, _jsonOptions);
    }

    public async Task SaveAsync(ManagedDevice device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        var payload = JsonSerializer.Serialize(device, _jsonOptions);
        var agentId = device.Agent is null ? null : device.Agent.AgentId.ToString("D");

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = """
            INSERT INTO Devices (Id, DeviceCode, DeviceName, AgentId, IsManaged, UpdatedAt, PayloadJson)
            VALUES ($id, $code, $name, $agentId, $isManaged, $updatedAt, $payload)
            ON CONFLICT(Id) DO UPDATE SET
                DeviceCode = excluded.DeviceCode,
                DeviceName = excluded.DeviceName,
                AgentId = excluded.AgentId,
                IsManaged = excluded.IsManaged,
                UpdatedAt = excluded.UpdatedAt,
                PayloadJson = excluded.PayloadJson;
            """;
        command.Parameters.AddWithValue("$id", device.Id.ToString("D"));
        command.Parameters.AddWithValue("$code", device.DeviceCode);
        command.Parameters.AddWithValue("$name", device.DeviceName);
        command.Parameters.AddWithValue("$agentId", (object?)agentId ?? DBNull.Value);
        command.Parameters.AddWithValue("$isManaged", device.IsManaged ? 1 : 0);
        command.Parameters.AddWithValue("$updatedAt", device.UpdatedAt.ToString("O"));
        command.Parameters.AddWithValue("$payload", payload);

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
