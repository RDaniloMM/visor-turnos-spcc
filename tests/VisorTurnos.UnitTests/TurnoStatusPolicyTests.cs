using Microsoft.Extensions.Options;
using VisorTurnos.Domain;
using VisorTurnos.Options;
using VisorTurnos.Services;

namespace VisorTurnos.UnitTests;

public sealed class TurnoStatusPolicyTests
{
    [Fact]
    public void ClosedStatusWithoutANewerOpenActIsClosed()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);
        var turno = Raw(status: "S", prefactura: 42, arrived: DateTime.Today);

        Assert.Equal(TurnoStatus.Cerrado, policy.Normalize(turno, DateTimeOffset.Now));
    }

    [Fact]
    public void LatestNumconInTReopensAnAppointmentEvenWhenStatteRemainsS()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);
        var turno = Raw(status: "S", prefactura: 42, arrived: DateTime.Today) with
        {
            HasMedicalConsultation = true,
            ConsultationStatus = "T",
            ConsultationId = 15892579
        };

        Assert.Equal(TurnoStatus.EnEspera, policy.Normalize(turno, DateTimeOffset.Now));
    }

    [Fact]
    public void LatestNumconInPClosesTheAppointment()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);
        var turno = Raw(status: "N", prefactura: 42, arrived: DateTime.Today) with
        {
            HasMedicalConsultation = true,
            ConsultationStatus = "P",
            ConsultationId = 15892429
        };

        Assert.Equal(TurnoStatus.Cerrado, policy.Normalize(turno, DateTimeOffset.Now));
    }

    [Fact]
    public void ValidPrefacturaAndMedicalConsultationMeansReadyToCall()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);
        var raw = Raw("N", 42, DateTime.Today) with
        {
            HasMedicalConsultation = true,
            ConsultationStatus = "T",
            ConsultationId = 100
        };

        Assert.Equal(TurnoStatus.EnEspera, policy.Normalize(raw, DateTimeOffset.Now));
    }

    [Fact]
    public void PrefacturaWithoutMedicalConsultationIsNotCallEligible()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);

        Assert.Equal(TurnoStatus.Desconocido, policy.Normalize(Raw("N", 42, DateTime.Today), DateTimeOffset.Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void MissingPrefacturaWithArrivalMeansEnEspera(int? prefactura)
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);

        Assert.Equal(TurnoStatus.EnEspera, policy.Normalize(Raw("N", prefactura, DateTime.Today), DateTimeOffset.Now));
    }

    [Fact]
    public void UnknownCombinationIsNotInvented()
    {
        var policy = CreatePolicy(zeroMeansAbsent: true);
        var old = DateTime.Today.AddDays(-1);

        Assert.Equal(TurnoStatus.Desconocido, policy.Normalize(Raw("X", null, null, old), DateTimeOffset.Now));
    }

    [Fact]
    public void ZeroPrefacturaIsUnknownUntilPolicyIsApproved()
    {
        var policy = CreatePolicy(zeroMeansAbsent: null);

        Assert.Equal(TurnoStatus.Desconocido, policy.Normalize(Raw("N", 0, DateTime.Today), DateTimeOffset.Now));
    }

    private static TurnoStatusPolicy CreatePolicy(bool? zeroMeansAbsent)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new BusinessRulesOptions
        {
            ClosedStatusCodes = ["S"],
            ZeroPrefacturaMeansAbsent = zeroMeansAbsent
        });
        return new TurnoStatusPolicy(new PrefacturaPolicy(options), options);
    }

    private static TurnoRaw Raw(string? status, int? prefactura, DateTime? arrived, DateTime? scheduled = null) =>
        new(1, "DEMO", "C1", "Medico", scheduled ?? DateTime.Today, arrived, status, prefactura, null, null, false);
}
