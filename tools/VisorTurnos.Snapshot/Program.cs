using System.Data;
using System.Data.Common;
using System.Data.Odbc;

// Carga .env (rama del repositorio hacia arriba) sin sobrescribir variables de
// entorno reales. Las claves SNAPSHOT_* deben vivir en .env o en el entorno; el
// nombre del servidor de produccion nunca se codifica en el repositorio.
DotNetEnv.Env.NoClobber().TraversePath().Load();

const int SiteCode = 1;
var sourceConnectionString = Environment.GetEnvironmentVariable("SNAPSHOT_SOURCE_CONNECTION")
    ?? throw new InvalidOperationException("Falta SNAPSHOT_SOURCE_CONNECTION. Definala en .env o en el entorno (Driver={ODBC Driver 18 for SQL Server};Server=<server>;Database=LOLCLI9000;Trusted_Connection=Yes;Encrypt=Yes;TrustServerCertificate=Yes;).");
var targetMasterConnectionString = Environment.GetEnvironmentVariable("SNAPSHOT_TARGET_MASTER_CONNECTION")
    ?? "Driver={ODBC Driver 18 for SQL Server};Server=(localdb)\\VisorTurnosDevelopment;Database=master;Trusted_Connection=Yes;TrustServerCertificate=Yes;";
var targetConnectionString = Environment.GetEnvironmentVariable("SNAPSHOT_TARGET_CONNECTION")
    ?? "Driver={ODBC Driver 18 for SQL Server};Server=(localdb)\\VisorTurnosDevelopment;Database=VisorTurnosDevelopment;Trusted_Connection=Yes;TrustServerCertificate=Yes;";

var siteZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, siteZone);
var dayStart = localNow.Date;
var dayEndExclusive = dayStart.AddDays(1);

var snapshot = await ReadSourceAsync(dayStart, dayEndExclusive, sourceConnectionString);
await EnsureDatabaseAsync(targetMasterConnectionString);
await ReplaceSnapshotAsync(snapshot, targetConnectionString);
Console.WriteLine(
    "Snapshot de desarrollo creado: {0} citas, {1} médicos, {2} consultorios, {3} consultas y {4} ausencias documentadas.",
    snapshot.Citas.Count,
    snapshot.Medicos.Count,
    snapshot.Consultorios.Count,
    snapshot.Consultas.Count,
    snapshot.NoShowDiagnosticos.Count);

static async Task<SnapshotData> ReadSourceAsync(DateTime dayStart, DateTime dayEndExclusive, string sourceConnectionString)
{
    await using var source = new OdbcConnection(sourceConnectionString);
    await source.OpenAsync();

    var citas = await ReadAsync(source, """
        SELECT c.invnum, c.pacnam, c.medcod, c.codcon, c.citdat, c.cithll,
               c.statte, c.tcicod, c.prfnum, c.obscit, c.siscod
        FROM dbo.citas AS c
        WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?
        ORDER BY c.citdat, c.invnum;
        """, command =>
    {
        Add(command, OdbcType.Int, SiteCode);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);
    }, reader => new Cita(
        reader.GetInt32(0), ReadString(reader, 1), ReadString(reader, 2), ReadString(reader, 3),
        reader.GetDateTime(4), ReadDate(reader, 5), ReadString(reader, 6), ReadString(reader, 7),
        ReadInt(reader, 8), ReadString(reader, 9), reader.GetInt32(10)));

    var medicos = await ReadAsync(source, """
        SELECT DISTINCT m.medcod, m.mednam, m.codcon
        FROM dbo.medicos AS m
        INNER JOIN dbo.citas AS c ON c.medcod = m.medcod
        WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?;
        """, command =>
    {
        Add(command, OdbcType.Int, SiteCode);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);
    }, reader => new Medico(ReadString(reader, 0), ReadString(reader, 1), ReadString(reader, 2)));

    var consultorios = await ReadAsync(source, """
        SELECT DISTINCT co.codcon, co.descon
        FROM dbo.consultorios AS co
        INNER JOIN dbo.medicos AS m ON m.codcon = co.codcon
        INNER JOIN dbo.citas AS c ON c.medcod = m.medcod
        WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?;
        """, command =>
    {
        Add(command, OdbcType.Int, SiteCode);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);
    }, reader => new Consultorio(ReadString(reader, 0), ReadString(reader, 1)));

    var consultas = await ReadAsync(source, """
        SELECT ac.numcon, ac.invnum, ac.prfnum, ac.stacon, ac.feccon, ac.feccre, ac.fecumv
        FROM dbo.am_consulta AS ac
        INNER JOIN dbo.citas AS c ON c.invnum = ac.invnum
        WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?;
        """, command =>
    {
        Add(command, OdbcType.Int, SiteCode);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);
    }, reader => new Consulta(
        reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), ReadString(reader, 3),
        ReadDate(reader, 4), ReadDate(reader, 5), ReadDate(reader, 6)));

    // La copia local solo conserva el indicador requerido por el visor; no
    // exporta los demás diagnósticos ni su descripción.
    var noShowDiagnosticos = await ReadAsync(source, """
        SELECT DISTINCT diagnosis.numcon, diagnosis.diacod
        FROM dbo.am_diagnosticos AS diagnosis
        INNER JOIN dbo.am_consulta AS consultation ON consultation.numcon = diagnosis.numcon
        INNER JOIN dbo.citas AS c ON c.invnum = consultation.invnum
        WHERE c.siscod = ? AND c.citdat >= ? AND c.citdat < ?
          AND UPPER(LTRIM(RTRIM(COALESCE(diagnosis.diacod, '')))) = 'Z53.8';
        """, command =>
    {
        Add(command, OdbcType.Int, SiteCode);
        Add(command, OdbcType.DateTime, dayStart);
        Add(command, OdbcType.DateTime, dayEndExclusive);
    }, reader => new NoShowDiagnostico(reader.GetInt32(0), ReadString(reader, 1) ?? "Z53.8"));

    return new SnapshotData(citas, medicos, consultorios, consultas, noShowDiagnosticos);
}

static async Task EnsureDatabaseAsync(string targetMasterConnectionString)
{
    await using var master = new OdbcConnection(targetMasterConnectionString);
    await master.OpenAsync();

    try
    {
        await ExecuteAsync(master, """
            IF DB_ID(N'VisorTurnosDevelopment') IS NULL
                CREATE DATABASE [VisorTurnosDevelopment];
            """);
    }
    catch (OdbcException error) when (IsOrphanedDevelopmentDataFile(error))
    {
        var dataDirectory = await GetInstanceDefaultDataDirectoryAsync(master);
        var dataFile = Path.Combine(dataDirectory, "VisorTurnosDevelopment.mdf");
        var escapedDataFile = dataFile.Replace("'", "''", StringComparison.Ordinal);

        await ExecuteAsync(master, $"""
            CREATE DATABASE [VisorTurnosDevelopment]
            ON (FILENAME = N'{escapedDataFile}')
            FOR ATTACH;
            """);
        Console.WriteLine("Se adjunto el archivo LocalDB existente de Development.");
    }
}

static bool IsOrphanedDevelopmentDataFile(OdbcException error) =>
    error.Message.Contains("VisorTurnosDevelopment.mdf", StringComparison.OrdinalIgnoreCase) &&
    error.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);

static async Task<string> GetInstanceDefaultDataDirectoryAsync(OdbcConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'));";
    var value = await command.ExecuteScalarAsync();
    var directory = Convert.ToString(value)?.Trim();

    if (string.IsNullOrWhiteSpace(directory))
    {
        throw new InvalidOperationException("LocalDB no informo su directorio de datos predeterminado.");
    }

    return directory;
}

static async Task ReplaceSnapshotAsync(SnapshotData snapshot, string targetConnectionString)
{
    await using var target = new OdbcConnection(targetConnectionString);
    await target.OpenAsync();
    using var transaction = target.BeginTransaction();

    await ExecuteAsync(target, """
        DROP TABLE IF EXISTS dbo.am_diagnosticos;
        DROP TABLE IF EXISTS dbo.am_consulta;
        DROP TABLE IF EXISTS dbo.citas;
        DROP TABLE IF EXISTS dbo.consultorios;
        DROP TABLE IF EXISTS dbo.medicos;

        CREATE TABLE dbo.medicos (
            medcod varchar(10) NOT NULL PRIMARY KEY,
            mednam nvarchar(120) NULL,
            codcon varchar(10) NULL
        );
        CREATE TABLE dbo.consultorios (
            codcon varchar(10) NOT NULL PRIMARY KEY,
            descon nvarchar(120) NULL
        );
        CREATE TABLE dbo.citas (
            invnum int NOT NULL PRIMARY KEY,
            pacnam nvarchar(120) NULL,
            medcod varchar(10) NULL,
            codcon varchar(10) NULL,
            citdat datetime2(3) NOT NULL,
            cithll datetime2(3) NULL,
            statte varchar(10) NULL,
            tcicod varchar(10) NULL,
            prfnum int NULL,
            obscit nvarchar(120) NULL,
            siscod int NOT NULL
        );
        CREATE TABLE dbo.am_consulta (
            numcon int NOT NULL PRIMARY KEY,
            invnum int NOT NULL,
            prfnum int NOT NULL,
            stacon varchar(20) NULL,
            feccon datetime2(3) NULL,
            feccre datetime2(3) NULL,
            fecumv datetime2(3) NULL
        );
        CREATE TABLE dbo.am_diagnosticos (
            numcon int NOT NULL PRIMARY KEY,
            diacod varchar(20) NOT NULL
        );
        CREATE INDEX IX_citas_site_date ON dbo.citas (siscod, citdat, invnum);
        CREATE INDEX IX_am_consulta_cita ON dbo.am_consulta (invnum, feccon, numcon);
        """, transaction);

    foreach (var medico in snapshot.Medicos)
    {
        await ExecuteAsync(target, "INSERT INTO dbo.medicos (medcod, mednam, codcon) VALUES (?, ?, ?);", transaction,
            medico.Code, medico.Name, medico.RoomCode);
    }
    foreach (var consultorio in snapshot.Consultorios)
    {
        await ExecuteAsync(target, "INSERT INTO dbo.consultorios (codcon, descon) VALUES (?, ?);", transaction,
            consultorio.Code, consultorio.Name);
    }
    foreach (var cita in snapshot.Citas)
    {
        await ExecuteAsync(target, """
            INSERT INTO dbo.citas (invnum, pacnam, medcod, codcon, citdat, cithll, statte, tcicod, prfnum, obscit, siscod)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
            """, transaction,
            cita.Invnum, cita.PatientName, cita.MedicalCode, cita.RoomCode, cita.ScheduledAt, cita.ArrivedAt,
            cita.Status, cita.CitedTypeCode, cita.PrefacturaNumber, cita.Observation, cita.SiteCode);
    }
    foreach (var consulta in snapshot.Consultas)
    {
        await ExecuteAsync(target, "INSERT INTO dbo.am_consulta (numcon, invnum, prfnum, stacon, feccon, feccre, fecumv) VALUES (?, ?, ?, ?, ?, ?, ?);", transaction,
            consulta.ConsultationId, consulta.AppointmentId, consulta.PrefacturaNumber, consulta.Status,
            consulta.ConnectedAt, consulta.CreatedAt, consulta.LastModifiedAt);
    }
    foreach (var diagnostico in snapshot.NoShowDiagnosticos)
    {
        await ExecuteAsync(target, "INSERT INTO dbo.am_diagnosticos (numcon, diacod) VALUES (?, ?);", transaction,
            diagnostico.ConsultationId, diagnostico.Code);
    }

    transaction.Commit();
}

static async Task<List<T>> ReadAsync<T>(OdbcConnection connection, string sql, Action<OdbcCommand> bind, Func<DbDataReader, T> map)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.CommandTimeout = 15;
    bind(command);
    await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult);
    var results = new List<T>();
    while (await reader.ReadAsync()) results.Add(map(reader));
    return results;
}

static async Task ExecuteAsync(OdbcConnection connection, string sql, OdbcTransaction? transaction = null, params object?[] values)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.CommandTimeout = 30;
    command.Transaction = transaction;
    foreach (var value in values) AddValue(command, value);
    await command.ExecuteNonQueryAsync();
}

static void Add(OdbcCommand command, OdbcType type, object? value)
{
    var parameter = command.CreateParameter();
    parameter.OdbcType = type;
    if (type == OdbcType.DateTime)
    {
        parameter.Scale = 3;
    }
    parameter.Value = value ?? DBNull.Value;
    command.Parameters.Add(parameter);
}

static void AddValue(OdbcCommand command, object? value)
{
    var type = value switch
    {
        int => OdbcType.Int,
        DateTime => OdbcType.DateTime,
        _ => OdbcType.NVarChar
    };
    Add(command, type, value);
}

static string? ReadString(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal).Trim();
static int? ReadInt(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
static DateTime? ReadDate(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);

sealed record SnapshotData(
    List<Cita> Citas,
    List<Medico> Medicos,
    List<Consultorio> Consultorios,
    List<Consulta> Consultas,
    List<NoShowDiagnostico> NoShowDiagnosticos);
sealed record Cita(int Invnum, string? PatientName, string? MedicalCode, string? RoomCode, DateTime ScheduledAt, DateTime? ArrivedAt, string? Status, string? CitedTypeCode, int? PrefacturaNumber, string? Observation, int SiteCode);
sealed record Medico(string? Code, string? Name, string? RoomCode);
sealed record Consultorio(string? Code, string? Name);
sealed record Consulta(int ConsultationId, int AppointmentId, int PrefacturaNumber, string? Status, DateTime? ConnectedAt, DateTime? CreatedAt, DateTime? LastModifiedAt);
sealed record NoShowDiagnostico(int ConsultationId, string Code);
