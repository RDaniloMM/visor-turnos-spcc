using Microsoft.Extensions.Options;
using VisorTurnos.Options;

namespace VisorTurnos.Services;

public sealed class PrefacturaPolicy(IOptions<BusinessRulesOptions> options)
{
    public PrefacturaPresence Evaluate(int? value)
    {
        if (!value.HasValue)
        {
            return PrefacturaPresence.Absent;
        }

        if (value.Value != 0)
        {
            return PrefacturaPresence.Present;
        }

        return options.Value.ZeroPrefacturaMeansAbsent switch
        {
            true => PrefacturaPresence.Absent,
            false => PrefacturaPresence.Present,
            null => PrefacturaPresence.Unknown
        };
    }
}

public enum PrefacturaPresence
{
    Unknown,
    Absent,
    Present
}
