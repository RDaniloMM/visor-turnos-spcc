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
    internal const string Sql = """
        WITH CitasDelDia AS
        (
            SELECT
                c.invnum,
                c.pacnam,
                c.medcod,
                c.codcon,
                c.citdat,
                c.cithll,
                c.statte,
                c.tcicod,
                c.prfnum,
                UPPER(LTRIM(RTRIM(REPLACE(COALESCE(c.obscit, ''), '"', '')))) AS observacion_normalizada
            FROM dbo.citas AS c
            WHERE c.siscod = ?
              AND c.citdat >= ?
              AND c.citdat < ?
        ),
        ActosRecientes AS
        (
            -- numcon es la clave secuencial del acto. Limitar la lectura a
            -- los últimos registros evita una búsqueda por invnum sobre toda
            -- am_consulta en cada sondeo.
            SELECT TOP (?)
                consultation.numcon,
                consultation.invnum,
                consultation.prfnum,
                consultation.stacon,
                consultation.feccon,
                consultation.feccre,
                consultation.fecumv
            FROM dbo.am_consulta AS consultation
            ORDER BY consultation.numcon DESC
        ),
        UltimoActoPorCita AS
        (
            SELECT
                consultation.numcon,
                COUNT(*) OVER (PARTITION BY consultation.invnum) AS attempt_count,
                consultation.invnum,
                consultation.prfnum,
                consultation.stacon,
                consultation.feccon,
                consultation.feccre,
                consultation.fecumv,
                ROW_NUMBER() OVER (
                    PARTITION BY consultation.invnum
                    ORDER BY consultation.numcon DESC) AS act_rank
            FROM ActosRecientes AS consultation
        )
        SELECT TOP (?)
            c.invnum,
            c.pacnam,
            c.codcon,
            c.citdat,
            c.cithll,
            c.statte,
            c.tcicod,
            c.medcod,
            c.prfnum,
            CAST(CASE
                WHEN c.observacion_normalizada = ? THEN 1
                ELSE 0
            END AS bit) AS is_medical_exam,
            CAST(CASE
                -- No usar rangos como [A-Z]: la intercalacion de LOLCLI no
                -- los evalua de forma consistente. Los codigos acordados se
                -- comparan de forma literal, despues de quitar comillas y
                -- espacios de obscit.
                WHEN c.observacion_normalizada IN
                    ('A', 'B', 'C', 'D', 'E', 'A1', 'B1', 'C1', 'D1', 'E1') THEN 1
                ELSE 0
            END AS bit) AS is_amanecida,
            m.mednam,
            co.descon,
            ac.numcon,
            ac.attempt_count,
            ac.invnum AS consultation_invnum,
            ac.prfnum AS consultation_prefactura_number,
            ac.stacon,
            ac.feccon,
            ac.feccre,
            ac.fecumv
        FROM CitasDelDia AS c
        INNER JOIN dbo.medicos AS m
            ON m.medcod = c.medcod
        LEFT JOIN dbo.consultorios AS co
            ON co.codcon = m.codcon
        LEFT JOIN UltimoActoPorCita AS ac
            ON ac.invnum = c.invnum
           AND ac.act_rank = 1
        WHERE c.citdat >= ?
           OR c.observacion_normalizada = ?
           -- Un médico puede abrir un acto después de la hora programada.
           -- El numcon vigente debe llegar al worker para que este detecte
           -- el evento; la cola evita anunciar actos que ya existían al
           -- inicio del proceso.
           OR (ac.numcon IS NOT NULL AND (ac.stacon IS NULL OR ac.stacon <> 'P'))
        ORDER BY CASE WHEN ac.numcon IS NOT NULL AND (ac.stacon IS NULL OR ac.stacon <> 'P') THEN 0 ELSE 1 END,
                 is_medical_exam DESC,
                 is_amanecida DESC,
                 c.citdat,
                 c.invnum;
        """;

    public async Task<IReadOnlyList<TurnoRaw>> GetForDayAsync(
        int siteCode,
        DateTime windowStart,
        DateTime dayEndExclusive,
        int maxRows,
        CancellationToken cancellationToken)
    {
        await using var connection = new OdbcConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = Sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = queueOptions.Value.CommandTimeoutSeconds;
        var medicalExamCode = priorityOptions.Value.MedicalExamObservationCode.Trim().ToUpperInvariant();
        AddParameter(command, OdbcType.Int, siteCode);
        AddParameter(command, OdbcType.DateTime, windowStart.Date);
        AddParameter(command, OdbcType.DateTime, dayEndExclusive);
        AddParameter(command, OdbcType.Int, queueOptions.Value.RecentConsultationRows);
        AddParameter(command, OdbcType.Int, Math.Min(maxRows, queueOptions.Value.MaxQueryRows));
        AddParameter(command, OdbcType.VarChar, medicalExamCode, 20);
        AddParameter(command, OdbcType.DateTime, windowStart);
        AddParameter(command, OdbcType.VarChar, medicalExamCode, 20);

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

            results.Add(MapTurno(reader, stableId, publicId));
        }

        return results;
    }

    internal static TurnoRaw MapTurno(DbDataReader reader, int stableId, string? publicId) =>
        new(
            stableId,
            publicId,
            ReadString(reader, 12) ?? ReadString(reader, 2) ?? "Por confirmar",
            ReadString(reader, 11) ?? "Médico por confirmar",
            reader.GetDateTime(3),
            ReadDateTime(reader, 4),
            ReadString(reader, 5),
            reader.IsDBNull(8) ? null : reader.GetInt32(8),
            ReadString(reader, 6),
            null,
            !reader.IsDBNull(9) && reader.GetBoolean(9),
            !reader.IsDBNull(13),
            ReadString(reader, 17),
            ReadDateTime(reader, 18),
            ReadDateTime(reader, 19),
            ReadDateTime(reader, 20),
            !reader.IsDBNull(10) && reader.GetBoolean(10),
            reader.IsDBNull(13) ? null : reader.GetInt32(13),
            reader.IsDBNull(14) ? 0 : reader.GetInt32(14));

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

    private static DateTime? ReadDateTime(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

}
