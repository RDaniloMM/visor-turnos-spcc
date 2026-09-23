using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.Data;

/// <summary>
/// Escrituras de prueba permitidas solo en LocalDB. Este repositorio nunca usa
/// la conexión LOLCLI ni se registra como fuente del visor público.
/// </summary>
public sealed class DevelopmentSimulationRepository(
    DevelopmentSnapshotGuard guard,
    IOptions<SiteOptions> siteOptions,
    IOptions<ScheduleOptions> scheduleOptions,
    ConsultorioExclusionPolicy consultorioExclusionPolicy,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<SimulationTurnDto>> GetTurnsAsync(CancellationToken cancellationToken)
    {
        var (dayStart, dayEndExclusive) = GetFullDayRange();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.invnum, c.medcod, c.pacnam, COALESCE(co.descon, c.codcon), m.mednam,
                   c.citdat, c.statte, c.obscit, c.prfnum,
                   ac.numcon, ac.consultation_prfnum, ac.stacon, ac.feccon, ac.feccre, ac.fecumv
            FROM dbo.citas AS c
            INNER JOIN dbo.medicos AS m ON m.medcod = c.medcod
            LEFT JOIN dbo.consultorios AS co ON co.codcon = m.codcon
            OUTER APPLY
            (
                SELECT TOP (1)
                       consultation.numcon,
                       consultation.prfnum AS consultation_prfnum,
                       consultation.stacon,
                       consultation.feccon,
                       consultation.feccre,
                       consultation.fecumv
                FROM dbo.am_consulta AS consultation
                WHERE consultation.invnum = c.invnum
                ORDER BY consultation.feccon DESC, consultation.numcon DESC
            ) AS ac
            WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?
            ORDER BY c.citdat, c.invnum;
            """;
        Add(command, OdbcType.Int, siteOptions.Value.Code);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
        var results = new List<SimulationTurnDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var consultorio = ReadString(reader, 3) ?? "Por confirmar";
            if (consultorioExclusionPolicy.IsExcluded(consultorio))
            {
                continue;
            }

            results.Add(new SimulationTurnDto(
                reader.GetInt32(0),
                ReadString(reader, 1) ?? string.Empty,
                ReadString(reader, 2) ?? "Sin nombre",
                consultorio,
                ReadString(reader, 4) ?? "Por confirmar",
                reader.GetDateTime(5),
                ReadString(reader, 6),
                ReadString(reader, 7),
                ReadInt(reader, 8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9),
                !reader.IsDBNull(10),
                ReadString(reader, 11),
                ReadDate(reader, 12),
                ReadDate(reader, 13),
                ReadDate(reader, 14)));
        }

        return results;
    }

    public async Task<SimulationActionResultDto> CreateFictitiousAppointmentAsync(
        CreateSimulationAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateFictitiousAppointment(request);
        if (validation.Error is not null)
        {
            return new SimulationActionResultDto(false, validation.Error);
        }

        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var roomCode = await GetRoomCodeAsync(connection, transaction, validation.MedicalCode!, cancellationToken);
        if (roomCode is null)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "El profesional seleccionado no existe en la copia local.");
        }

        var invnum = await GetNextAppointmentNumberAsync(connection, transaction, cancellationToken);
        var patientName = $"PACIENTE DE PRUEBA {invnum}";
        await ExecuteAsync(connection, transaction, """
            INSERT INTO dbo.citas
                (invnum, pacnam, medcod, codcon, citdat, cithll, statte, tcicod, prfnum, obscit, siscod)
            VALUES (?, ?, ?, ?, ?, NULL, 'N', 'SIM', 0, ?, ?);
            """, cancellationToken,
            invnum,
            patientName,
            validation.MedicalCode!,
            roomCode,
            validation.ScheduledAt!.Value,
            validation.Observation!,
            siteOptions.Value.Code);
        transaction.Commit();

        return new SimulationActionResultDto(
            true,
            $"Se agregó {patientName} como {validation.Label.ToLowerInvariant()}, sin prefactura ni acto médico.",
            invnum);
    }

    public async Task<SimulationActionResultDto> OpenMedicalActAsync(int invnum, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var alreadyOpen = await HasOpenMedicalActAsync(connection, transaction, invnum, cancellationToken);
        if (alreadyOpen)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "La cita ya tiene un acto médico T abierto.");
        }

        var affected = await ExecuteAsync(connection, transaction, """
            UPDATE dbo.citas
            SET statte = 'N'
            WHERE invnum = ?;
            """, cancellationToken, invnum);
        if (affected != 1)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "La cita local no existe o no pudo reabrirse.");
        }

        var consultationId = await GetNextConsultationNumberAsync(connection, transaction, cancellationToken);
        var now = DateTime.Now;
        await ExecuteAsync(connection, transaction, """
            INSERT INTO dbo.am_consulta (numcon, invnum, prfnum, stacon, feccon, feccre, fecumv)
            VALUES (?, ?, ?, 'T', ?, ?, ?);
            """, cancellationToken, consultationId, invnum, 0, now, now, now);
        transaction.Commit();
        return new SimulationActionResultDto(
            true,
            $"Acto médico {consultationId} creado. El visor evalúa si corresponde anunciarlo; la prefactura es opcional en esta simulación.");
    }

    public async Task<SimulationActionResultDto> CreatePrefacturaAsync(int invnum, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        if (!await HasOpenMedicalActAsync(connection, transaction, invnum, cancellationToken))
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "Primero abre el acto médico para crear la prefactura.");
        }

        var current = await GetPrefacturaAsync(connection, transaction, invnum, cancellationToken);
        if (current is > 0)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "La cita ya tiene una prefactura válida.");
        }

        var prefactura = await GetNextPrefacturaAsync(connection, transaction, cancellationToken);
        var updated = await ExecuteAsync(connection, transaction, """
            UPDATE dbo.citas
            SET prfnum = ?
            WHERE invnum = ?
              AND (prfnum IS NULL OR prfnum = 0);
            """, cancellationToken, prefactura, invnum);
        if (updated != 1)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "No se pudo crear la prefactura local.");
        }

        transaction.Commit();
        return new SimulationActionResultDto(true, $"Prefactura {prefactura} creada en citas.prfnum.");
    }

    public async Task<SimulationActionResultDto> SaveConsultationAsync(int invnum, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var now = DateTime.Now;
        var updated = await ExecuteAsync(connection, transaction, """
            UPDATE consultation
            SET stacon = 'P', fecumv = ?
            FROM dbo.am_consulta AS consultation
            WHERE consultation.numcon =
            (
                SELECT TOP (1) latest.numcon
                FROM dbo.am_consulta AS latest
                WHERE latest.invnum = ?
                ORDER BY latest.feccon DESC, latest.numcon DESC
            )
              AND consultation.stacon = 'T';
            """, cancellationToken, now, invnum);
        if (updated != 1)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "El turno no tiene una consulta local abierta o ya fue guardado.");
        }
        var closed = await ExecuteAsync(connection, transaction,
            "UPDATE dbo.citas SET statte = 'S' WHERE invnum = ? AND (statte IS NULL OR statte <> 'S');",
            cancellationToken, invnum);
        if (closed != 1)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "No se pudo cerrar la cita local.");
        }
        transaction.Commit();
        return new SimulationActionResultDto(true, "El último numcon pasó de T a P y la cita pasó a statte=S.");
    }

    public async Task<SimulationActionResultDto> CloseAppointmentAsync(int invnum, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var updatedConsultation = await ExecuteAsync(connection, transaction, """
            UPDATE ac
            SET stacon = 'P', fecumv = ?
            FROM dbo.am_consulta AS ac
            WHERE ac.numcon =
            (
                SELECT TOP (1) latest.numcon
                FROM dbo.am_consulta AS latest
                WHERE latest.invnum = ?
                ORDER BY latest.feccon DESC, latest.numcon DESC
            )
              AND ac.stacon = 'T';
            """, cancellationToken, DateTime.Now, invnum);
        var updatedAppointment = await ExecuteAsync(connection, transaction,
            "UPDATE dbo.citas SET statte = 'S' WHERE invnum = ? AND statte <> 'S';",
            cancellationToken, invnum);
        transaction.Commit();
        return updatedAppointment == 1
            ? new SimulationActionResultDto(true, updatedConsultation > 0
                ? "Consulta y cita local cerradas."
                : "La cita local fue cerrada.")
            : new SimulationActionResultDto(false, "La cita ya estaba cerrada o no existe.");
    }

    public async Task<SimulationActionResultDto> ResetAppointmentAsync(int invnum, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await ExecuteAsync(connection, transaction, """
            DELETE FROM dbo.am_diagnosticos
            WHERE numcon IN (SELECT numcon FROM dbo.am_consulta WHERE invnum = ?);
            """, cancellationToken, invnum);
        await ExecuteAsync(connection, transaction, "DELETE FROM dbo.am_consulta WHERE invnum = ?;", cancellationToken, invnum);
        var updated = await ExecuteAsync(connection, transaction,
            "UPDATE dbo.citas SET prfnum = 0, statte = 'N' WHERE invnum = ?;",
            cancellationToken, invnum);
        if (updated != 1)
        {
            transaction.Rollback();
            return new SimulationActionResultDto(false, "La cita local no existe.");
        }
        transaction.Commit();
        return new SimulationActionResultDto(true, "Turno local restablecido; se eliminaron sus numcon de prueba.");
    }

    private async Task<OdbcConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new OdbcConnection(guard.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<int> GetNextAppointmentNumberAsync(
        OdbcConnection connection,
        OdbcTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ISNULL(MAX(invnum), 9000000) + 1 FROM dbo.citas;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> GetNextConsultationNumberAsync(
        OdbcConnection connection,
        OdbcTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ISNULL(MAX(numcon), 15000000) + 1 FROM dbo.am_consulta;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<bool> HasOpenMedicalActAsync(
        OdbcConnection connection,
        OdbcTransaction transaction,
        int invnum,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (1) stacon
            FROM dbo.am_consulta
            WHERE invnum = ?
            ORDER BY feccon DESC, numcon DESC;
            """;
        Add(command, OdbcType.Int, invnum);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return string.Equals(Convert.ToString(value)?.Trim(), "T", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> GetRoomCodeAsync(
        OdbcConnection connection,
        OdbcTransaction transaction,
        string medicalCode,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT TOP (1) codcon FROM dbo.medicos WHERE medcod = ?;";
        Add(command, OdbcType.VarChar, medicalCode);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value)?.Trim();
    }

    private static async Task<int?> GetPrefacturaAsync(OdbcConnection connection, OdbcTransaction transaction, int invnum, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT prfnum FROM dbo.citas WHERE invnum = ?;";
        Add(command, OdbcType.Int, invnum);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> ExecuteAsync(OdbcConnection connection, OdbcTransaction? transaction, string sql, CancellationToken cancellationToken, params object[] values)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 5;
        foreach (var value in values)
        {
            Add(command, value is int ? OdbcType.Int : value is DateTime ? OdbcType.DateTime : OdbcType.NVarChar, value);
        }
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private (DateTime Start, DateTime EndExclusive) GetFullDayRange()
    {
        var siteZone = TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZone);
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), siteZone);
        var start = local.Date;
        return (start, start.AddDays(1));
    }

    private (string? Error, string? MedicalCode, DateTime? ScheduledAt, string? Observation, string Label)
        ValidateFictitiousAppointment(CreateSimulationAppointmentRequest request)
    {
        var medicalCode = request.MedicalCode?.Trim();
        if (string.IsNullOrWhiteSpace(medicalCode))
        {
            return ("Selecciona un profesional de la copia local.", null, null, null, string.Empty);
        }

        if (!request.ScheduledAt.HasValue)
        {
            return ("Indica una hora para la cita ficticia.", null, null, null, string.Empty);
        }

        var scheduledAt = DateTime.SpecifyKind(request.ScheduledAt.Value, DateTimeKind.Unspecified);
        var siteZone = TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZone);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), siteZone);
        var dayStart = localNow.Date;
        var dayEndExclusive = dayStart.AddDays(1);
        if (scheduledAt < dayStart || scheduledAt >= dayEndExclusive)
        {
            return ("La cita ficticia debe programarse para hoy en la copia local.", null, null, null, string.Empty);
        }

        var time = scheduledAt.TimeOfDay;
        var schedule = scheduleOptions.Value;
        var morning = time >= TimeSpan.FromHours(schedule.MorningStartHour) &&
                      time < TimeSpan.FromHours(schedule.RecessStartHour);
        var afternoon = time >= TimeSpan.FromHours(schedule.AfternoonStartHour) &&
                        time < TimeSpan.FromHours(schedule.DayEndHour);
        if (!morning && !afternoon)
        {
            return ("La cita ficticia debe estar dentro del turno mañana o turno tarde.", null, null, null, string.Empty);
        }

        return request.Priority?.Trim().ToLowerInvariant() switch
        {
            "ema" => (null, medicalCode, scheduledAt, "EMA", "EMA"),
            "amanecida" when morning => (null, medicalCode, scheduledAt, "A", "amanecida"),
            "amanecida" => ("La amanecida de mañana debe programarse antes de las 12:00.", null, null, null, string.Empty),
            "amanecida-tarde" when afternoon => (null, medicalCode, scheduledAt, "A1", "amanecida de tarde"),
            "amanecida-tarde" => ("La amanecida de tarde debe programarse desde las 14:00.", null, null, null, string.Empty),
            "normal" => (null, medicalCode, scheduledAt, "PRESENCIAL", "turno normal"),
            _ => ("Selecciona una prioridad válida para la cita ficticia.", null, null, null, string.Empty)
        };
    }

    private static void Add(OdbcCommand command, OdbcType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.OdbcType = type;
        if (type == OdbcType.DateTime) parameter.Scale = 3;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<int> GetNextPrefacturaAsync(OdbcConnection connection, OdbcTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT ISNULL(MAX(prfnum), 8000000) + 1 FROM dbo.citas;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? ReadString(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
    private static int? ReadInt(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static DateTime? ReadDate(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
