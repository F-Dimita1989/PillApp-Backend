using Microsoft.EntityFrameworkCore;
using PillApp.Api.Models;

namespace PillApp.Api.Helpers;

public static class FarmacoSearchQuery
{
    public static IQueryable<FarmacoClasseA> Apply(IQueryable<FarmacoClasseA> query, string q, bool useILike)
    {
        if (useILike)
        {
            var pattern = $"%{q}%";
            return query.Where(f =>
                (f.PrincipioAttivo != null && EF.Functions.ILike(f.PrincipioAttivo, pattern)) ||
                EF.Functions.ILike(f.DenominazioneConfezione, pattern) ||
                (f.DescrizioneGruppo != null && EF.Functions.ILike(f.DescrizioneGruppo, pattern)));
        }

        var term = q.Trim().ToLowerInvariant();
        return query.Where(f =>
            (f.PrincipioAttivo != null && f.PrincipioAttivo.ToLower().Contains(term)) ||
            f.DenominazioneConfezione.ToLower().Contains(term) ||
            (f.DescrizioneGruppo != null && f.DescrizioneGruppo.ToLower().Contains(term)));
    }
}
