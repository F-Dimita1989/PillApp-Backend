namespace PillApp.Api.Dtos;

public class FarmacoLookupDto
{
    public string Aic { get; set; } = string.Empty;
    public string? PrincipioAttivo { get; set; }
    public string? DescrizioneGruppo { get; set; }
    public string DenominazioneConfezione { get; set; } = string.Empty;
    public decimal? PrezzoPubblico { get; set; }
    public string? TitolareAic { get; set; }
    public string? CodiceGruppoEquivalenza { get; set; }
    public bool InListaTrasparenzaAifa { get; set; }
    public string? SoloListaRegione { get; set; }
    public decimal? MetriCubiOssigeno { get; set; }
}