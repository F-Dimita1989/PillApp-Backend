using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PillApp.Api.Models;

[Table("farmaci_classe_a")]
public class FarmacoClasseA
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("principio_attivo")]
    public string? PrincipioAttivo { get; set; }

    [Column("descrizione_gruppo")]
    public string? DescrizioneGruppo { get; set; }

    [Required]
    [Column("denominazione_confezione")]
    public string DenominazioneConfezione { get; set; } = string.Empty;

    [Column("prezzo_pubblico")]
    public decimal? PrezzoPubblico { get; set; }

    [Column("titolare_aic")]
    public string? TitolareAic { get; set; }

    [Required]
    [Column("aic")]
    public string Aic { get; set; } = string.Empty;

    [Column("codice_gruppo_equivalenza")]
    public string? CodiceGruppoEquivalenza { get; set; }

    [Column("in_lista_trasparenza_aifa")]
    public bool InListaTrasparenzaAifa { get; set; }

    [Column("solo_lista_regione")]
    public string? SoloListaRegione { get; set; }

    [Column("metri_cubi_ossigeno")]
    public decimal? MetriCubiOssigeno { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}