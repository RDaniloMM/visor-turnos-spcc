using System.Data;
using VisorTurnos.Data;

namespace VisorTurnos.UnitTests;

public sealed class OdbcTurnosRepositoryTests
{
    [Fact]
    public void Sql_ReadsFromFilteredCte()
    {
        Assert.Contains("FROM CitasDelDia AS c", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("WHERE c.siscod = ?", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN UltimoActoPorCita AS ac", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("FROM dbo.citas AS c\n        INNER JOIN", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Sql_ReadsOnlyRecentActsAndSelectsTheLatestOnePerAppointment()
    {
        Assert.Contains("consultation.numcon", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("ActosRecientes", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("SELECT TOP (?)", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("COUNT(*) OVER (PARTITION BY consultation.invnum) AS attempt_count", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("ROW_NUMBER() OVER", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.Contains("ON ac.invnum = c.invnum", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("consultation.prfnum = c.prfnum", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("AND consultation.stacon = 'T'", OdbcTurnosRepository.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void MapTurno_UsesTheSelectedColumnOrdinals()
    {
        var scheduledAt = new DateTime(2026, 9, 18, 11, 30, 0);
        var connectedAt = scheduledAt.AddMinutes(1);
        var createdAt = scheduledAt.AddMinutes(2);
        var modifiedAt = scheduledAt.AddMinutes(3);
        var table = new DataTable();
        table.Columns.Add("invnum", typeof(int));
        table.Columns.Add("pacnam", typeof(string));
        table.Columns.Add("codcon", typeof(string));
        table.Columns.Add("citdat", typeof(DateTime));
        table.Columns.Add("cithll", typeof(DateTime));
        table.Columns.Add("statte", typeof(string));
        table.Columns.Add("tcicod", typeof(string));
        table.Columns.Add("medcod", typeof(string));
        table.Columns.Add("prfnum", typeof(int));
        table.Columns.Add("is_medical_exam", typeof(bool));
        table.Columns.Add("is_amanecida", typeof(bool));
        table.Columns.Add("mednam", typeof(string));
        table.Columns.Add("descon", typeof(string));
        table.Columns.Add("numcon", typeof(int));
        table.Columns.Add("attempt_count", typeof(int));
        table.Columns.Add("consultation_invnum", typeof(int));
        table.Columns.Add("consultation_prefactura_number", typeof(int));
        table.Columns.Add("stacon", typeof(string));
        table.Columns.Add("feccon", typeof(DateTime));
        table.Columns.Add("feccre", typeof(DateTime));
        table.Columns.Add("fecumv", typeof(DateTime));
        table.Rows.Add(
            42, "PACIENTE PRUEBA", "C01", scheduledAt, DBNull.Value, "N", "TC", "M01", 123,
            true, true, "MEDICO PRUEBA", "CONSULTORIO PRUEBA", 9001, 2, 42, 123, "T",
            connectedAt, createdAt, modifiedAt);

        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var result = OdbcTurnosRepository.MapTurno(reader, 42, "PACIENTE PRUEBA");

        Assert.Equal("CONSULTORIO PRUEBA", result.Consultorio);
        Assert.Equal("MEDICO PRUEBA", result.Medico);
        Assert.Equal(123, result.PrefacturaNumber);
        Assert.True(result.IsMedicalExam);
        Assert.True(result.IsAmanecida);
        Assert.True(result.HasMedicalConsultation);
        Assert.Equal(9001, result.ConsultationId);
        Assert.Equal(2, result.ConsultationAttemptCount);
        Assert.Equal("T", result.ConsultationStatus);
        Assert.Equal(connectedAt, result.ConsultationConnectedAt);
        Assert.Equal(createdAt, result.ConsultationCreatedAt);
        Assert.Equal(modifiedAt, result.ConsultationLastModifiedAt);
    }
}
