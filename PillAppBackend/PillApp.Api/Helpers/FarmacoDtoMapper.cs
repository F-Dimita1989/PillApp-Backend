using System.Linq.Expressions;
using PillApp.Api.Dtos;
using PillApp.Api.Models;

namespace PillApp.Api.Helpers;

public static class FarmacoDtoMapper
{
    public static readonly Expression<Func<FarmacoClasseA, FarmacoLookupDto>> ToLookupDto = f => new FarmacoLookupDto
    {
        Aic = f.Aic,
        PrincipioAttivo = f.PrincipioAttivo,
        DescrizioneGruppo = f.DescrizioneGruppo,
        DenominazioneConfezione = f.DenominazioneConfezione,
        PrezzoPubblico = f.PrezzoPubblico,
        TitolareAic = f.TitolareAic,
        CodiceGruppoEquivalenza = f.CodiceGruppoEquivalenza,
        InListaTrasparenzaAifa = f.InListaTrasparenzaAifa,
        SoloListaRegione = f.SoloListaRegione,
        MetriCubiOssigeno = f.MetriCubiOssigeno
    };

    public static FarmacoLookupDto ToDto(FarmacoClasseA entity) => new()
    {
        Aic = entity.Aic,
        PrincipioAttivo = entity.PrincipioAttivo,
        DescrizioneGruppo = entity.DescrizioneGruppo,
        DenominazioneConfezione = entity.DenominazioneConfezione,
        PrezzoPubblico = entity.PrezzoPubblico,
        TitolareAic = entity.TitolareAic,
        CodiceGruppoEquivalenza = entity.CodiceGruppoEquivalenza,
        InListaTrasparenzaAifa = entity.InListaTrasparenzaAifa,
        SoloListaRegione = entity.SoloListaRegione,
        MetriCubiOssigeno = entity.MetriCubiOssigeno
    };
}
