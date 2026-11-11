using System.Globalization;
using System.IO;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;

using Microsoft.Data.Sqlite;

namespace LANRescue.Infrastructure.Persistence;

public sealed class SqliteIpConflictRepository
    : IIpConflictRepository
{
    private readonly string _connectionString;

    public SqliteIpConflictRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException(
                "Database path is required.",
                nameof(databasePath));
        }

        var fullPath = Path.GetFullPath(databasePath);

        var directory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException(
                "Database directory is invalid.",
                nameof(databasePath));
        }

        Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            PRAGMA journal_mode = WAL;

            CREATE TABLE IF NOT EXISTS IpAddressObservations
            (
                Id TEXT NOT NULL PRIMARY KEY,
                AdapterId TEXT NOT NULL,
                AdapterName TEXT NOT NULL,
                SourceAddress TEXT NOT NULL,
                IpAddress TEXT NOT NULL,
                MacAddress TEXT NOT NULL,
                ProbeMethod TEXT NOT NULL,
                ObservedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS
                IX_IpObservations_Adapter_Source_Ip_Time
            ON IpAddressObservations
                (AdapterId, SourceAddress, IpAddress, ObservedAt);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveObservationsAsync(
        IReadOnlyList<IpAddressObservation> observations,
        CancellationToken cancellationToken = default)
    {
        if (observations.Count == 0)
            return;

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = """
            INSERT OR IGNORE INTO IpAddressObservations
            (
                Id,
                AdapterId,
                AdapterName,
                SourceAddress,
                IpAddress,
                MacAddress,
                ProbeMethod,
                ObservedAt
            )
            VALUES
            (
                $id,
                $adapterId,
                $adapterName,
                $sourceAddress,
                $ipAddress,
                $macAddress,
                $probeMethod,
                $observedAt
            );
            """;

        foreach (var item in observations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            command.Parameters.Clear();

            command.Parameters.AddWithValue(
                "$id", item.Id.ToString("D"));

            command.Parameters.AddWithValue(
                "$adapterId", item.AdapterId);

            command.Parameters.AddWithValue(
                "$adapterName", item.AdapterName);

            command.Parameters.AddWithValue(
                "$sourceAddress", item.SourceAddress);

            command.Parameters.AddWithValue(
                "$ipAddress", item.IpAddress);

            command.Parameters.AddWithValue(
                "$macAddress", item.MacAddress);

            command.Parameters.AddWithValue(
                "$probeMethod", item.ProbeMethod);

            command.Parameters.AddWithValue(
                "$observedAt",
                item.ObservedAt
                    .ToUniversalTime()
                    .ToString("O", CultureInfo.InvariantCulture));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<IReadOnlyList<IpAddressObservation>>
        GetRecentObservationsAsync(
            string adapterId,
            string sourceAddress,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                AdapterId,
                AdapterName,
                SourceAddress,
                IpAddress,
                MacAddress,
                ProbeMethod,
                ObservedAt
            FROM IpAddressObservations
            WHERE AdapterId = $adapterId COLLATE NOCASE
              AND SourceAddress = $sourceAddress COLLATE NOCASE
              AND ObservedAt >= $sinceUtc
            ORDER BY ObservedAt ASC;
            """;

        command.Parameters.AddWithValue(
            "$adapterId", adapterId);

        command.Parameters.AddWithValue(
            "$sourceAddress", sourceAddress);

        command.Parameters.AddWithValue(
            "$sinceUtc",
            sinceUtc.ToUniversalTime()
                .ToString("O", CultureInfo.InvariantCulture));

        var results = new List<IpAddressObservation>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new IpAddressObservation
            {
                Id = Guid.Parse(reader.GetString(0)),
                AdapterId = reader.GetString(1),
                AdapterName = reader.GetString(2),
                SourceAddress = reader.GetString(3),
                IpAddress = reader.GetString(4),
                MacAddress = reader.GetString(5),
                ProbeMethod = reader.GetString(6),
                ObservedAt = DateTimeOffset.Parse(
                    reader.GetString(7),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind)
            });
        }

        return results.AsReadOnly();
    }
}