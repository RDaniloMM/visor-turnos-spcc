using System.Security.Cryptography;
using System.Text;
using VisorTurnos.Domain;

namespace VisorTurnos.Services;

public sealed class TurnosChangeDetector
{
    public bool HasVisibleChange(TurnosSnapshotDto current, TurnosSnapshotDto candidate) =>
        !string.Equals(GetFingerprint(current), GetFingerprint(candidate), StringComparison.Ordinal);

    public string GetFingerprint(TurnosSnapshotDto snapshot)
    {
        var content = new StringBuilder(snapshot.Status);
        foreach (var item in snapshot.Items)
        {
            content.Append('\u001f').Append(item.PublicId)
                .Append('\u001f').Append(item.Consultorio)
                .Append('\u001f').Append(item.Medico)
                .Append('\u001f').Append(item.Estado)
                .Append('\u001f').Append(item.PriorityTier)
                .Append('\u001f').Append(item.IsPreferential)
                .Append('\u001f').Append(item.IsMedicalExam)
                .Append('\u001f').Append(item.ScheduledAt?.ToUnixTimeMilliseconds())
                .Append('\u001f').Append(item.ArrivedAt?.ToUnixTimeMilliseconds())
                .Append('\u001f').Append(item.ShouldAnnounce)
                .Append('\u001f').Append(item.IsActiveCall);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
    }
}
