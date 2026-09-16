namespace VisorTurnos.Domain;

public enum TurnoStatus
{
    Desconocido,
    PendienteLlegada,
    EnEspera,
    EnAtencion,
    Cerrado
}

public static class TurnoStatusNames
{
    public static string ToPublicName(this TurnoStatus status) => status switch
    {
        TurnoStatus.PendienteLlegada => "pendiente",
        TurnoStatus.EnEspera => "en-espera",
        TurnoStatus.EnAtencion => "en-atencion",
        TurnoStatus.Cerrado => "cerrado",
        _ => "desconocido"
    };
}
