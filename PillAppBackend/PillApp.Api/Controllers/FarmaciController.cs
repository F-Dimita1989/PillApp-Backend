using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PillApp.Api.Data;
using PillApp.Api.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace PillApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FarmaciController : ControllerBase
{
    private readonly AppDbContext _db;

    public FarmaciController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{aic}")]
    public async Task<ActionResult<FarmacoLookupDto>> GetByAic(string aic)
    {
        if (string.IsNullOrWhiteSpace(aic))
            return BadRequest("AIC non valido.");

        var farmaco = await _db.FarmaciClasseA
            .AsNoTracking()
            .Where(f => f.Aic == aic)
            .Select(f => new FarmacoLookupDto
            {
                Aic = f.Aic,
                PrincipioAttivo = f.PrincipioAttivo,
                DenominazioneConfezione = f.DenominazioneConfezione,
                PrezzoPubblico = f.PrezzoPubblico,
                TitolareAic = f.TitolareAic,
                CodiceGruppoEquivalenza = f.CodiceGruppoEquivalenza,
                InListaTrasparenzaAifa = f.InListaTrasparenzaAifa,
                SoloListaRegione = f.SoloListaRegione,
                MetriCubiOssigeno = f.MetriCubiOssigeno
            })
            .FirstOrDefaultAsync();

        if (farmaco == null)
            return NotFound(new { messaggio = $"Nessun farmaco trovato per AIC {aic}" });

        return Ok(farmaco);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<FarmacoLookupDto>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Inserisci un termine di ricerca.");

        var result = await _db.FarmaciClasseA
            .AsNoTracking()
            .Where(f =>
                (f.PrincipioAttivo != null && EF.Functions.ILike(f.PrincipioAttivo, $"%{q}%")) ||
                EF.Functions.ILike(f.DenominazioneConfezione, $"%{q}%"))
            .OrderBy(f => f.DenominazioneConfezione)
            .Take(20)
            .Select(f => new FarmacoLookupDto
            {
                Aic = f.Aic,
                PrincipioAttivo = f.PrincipioAttivo,
                DenominazioneConfezione = f.DenominazioneConfezione,
                PrezzoPubblico = f.PrezzoPubblico,
                TitolareAic = f.TitolareAic,
                CodiceGruppoEquivalenza = f.CodiceGruppoEquivalenza,
                InListaTrasparenzaAifa = f.InListaTrasparenzaAifa,
                SoloListaRegione = f.SoloListaRegione,
                MetriCubiOssigeno = f.MetriCubiOssigeno
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("test-connessione")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> TestConnessione()
    {
        var count = await _db.FarmaciClasseA.CountAsync();

        return Ok(new
        {
            messaggio = "Connessione al database riuscita",
            totaleRecord = count
        });
    }
}