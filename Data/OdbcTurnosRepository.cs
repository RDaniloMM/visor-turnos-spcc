using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;

namespace VisorTurnos.Data;

public sealed class OdbcTurnosRepository(
    string connectionString,
    IOptions<QueueOptions> queueOptions,
    IOptions<BusinessRulesOptions> businessRulesOptions,
    IOptions<PriorityOptions> priorityOptions) : ITurnosRepository
{
    private const string Sql = """
        SELECT TOP (?)
            c.invnum,
            c.pacnam,
            c.codcon,
            c.citdat,
            c.cithll,
            c.statte,
            c.tcicod,
            c.prfnum,
            CAST(CASE
                WHEN LTRIM(RTRIM(c.obscit)) = ? THEN 1
                ELSE 0
            END AS bit) AS is_medical_exam,
            m.mednam,
            co.descon
        FROM dbo.citas AS c
        INNER JOIN dbo.medicos AS m
            ON m.medcod = c.medcod
        LEFT JOIN dbo.consultorios AS co
            ON co.codcon = m.codcon
        WHERE c.siscod = ?
          AND c.citdat >= ?
          AND c.citdat < ?
        ORDER BY CASE WHEN c.statte = ? THEN 1 ELSE 0 END,
                 c.citdat,
                 c.invnum;
        """;

    public async Task<IReadOnlyList<TurnoRaw>> GetForDayAsync(
        int siteCode,
        DateTime dayStart,
        DateTime nextDayStart,
        int maxRows,
        CancellationToken cancellationToken)
    {
        await using var connection = new OdbcConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = queueOptions.Value.CommandTimeoutSeconds;
        AddParameter(command, OdbcType.Int, Math.Min(maxRows, queueOptions.Value.MaxQueryRows));
        AddParameter(command, OdbcType.VarChar, priorityOptions.Value.MedicalExamObservationCode, 20);
        AddParameter(command, OdbcType.Int, siteCode);
        AddParameter(command, OdbcType.DateTime, dayStart);
        AddParameter(command, OdbcType.DateTime, nextDayStart);
        AddParameter(command, OdbcType.VarChar, GetClosedStatusCode(), 2);

        var results = new List<TurnoRaw>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SingleResult,
            cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var stableId = reader.GetInt32(0);
            var identifierMode = businessRulesOptions.Value.PublicIdentifierMode;
            var publicId = identifierMode switch
            {
                PublicIdentifierMode.PatientName => ReadString(reader, 1),
                PublicIdentifierMode.Invnum => stableId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => null
            };

            results.Add(new TurnoRaw(
                stableId,
                publicId,
                ReadString(reader, 10) ?? ReadString(reader, 2) ?? "Por confirmar",
                ReadString(reader, 9) ?? "Médico por confirmar",
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                ReadString(reader, 5),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                ReadString(reader, 6),
                null,
                !reader.IsDBNull(8) && reader.GetBoolean(8)));
        }

        return results;
    }

    private static void AddParameter(OdbcCommand command, OdbcType type, object value, int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.OdbcType = type;
        if (size.HasValue)
        {
            parameter.Size = size.Value;
        }
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string? ReadString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();

    private string GetClosedStatusCode()
    {
        var closedStatusCode = businessRulesOptions.Value.ClosedStatusCodes
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return closedStatusCode?.Trim()
            ?? throw new InvalidOperationException("No se configuro un estado cerrado para la consulta ODBC.");
    }
}
