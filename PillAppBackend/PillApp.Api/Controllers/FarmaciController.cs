using Microsoft.AspNetCore.Mvc;
using PillApp.Api.Dtos;
using PillApp.Api.Services;

namespace PillApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FarmaciController : ControllerBase
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;
    private const int MinSearchLength = 3;
    private const int ClientCacheSeconds = 3600;

    private readonly FarmaciReadService _farmaci;

    public FarmaciController(FarmaciReadService farmaci)
    {
        _farmaci = farmaci;
    }

    [HttpGet("search")]
    public async Task<ActionResult<FarmacoSearchResultDto>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = DefaultLimit,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Il termine di ricerca è obbligatorio." });

        // Sotto i 3 caratteri gli indici trigram non vengono usati e la query degenera
        // in una scansione completa della tabella.
        if (q.Trim().Length < MinSearchLength)
            return BadRequest(new { error = $"Il termine di ricerca deve contenere almeno {MinSearchLength} caratteri." });

        if (limit < 1 || limit > MaxLimit)
            return BadRequest(new { error = $"Il parametro limit deve essere compreso tra 1 e {MaxLimit}." });

        if (offset < 0)
            return BadRequest(new { error = "Il parametro offset deve essere maggiore o uguale a zero." });

        var result = await _farmaci.SearchAsync(q, limit, offset, cancellationToken);

        SetClientCacheHeader();
        return Ok(result);
    }

    [HttpGet("{aic}")]
    public async Task<ActionResult<FarmacoLookupDto>> GetByAic(
        string aic,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aic))
            return BadRequest(new { error = "Codice AIC non valido." });

        var farmaco = await _farmaci.GetByAicAsync(aic, cancellationToken);

        if (farmaco == null)
            return NotFound(new { error = $"Nessun farmaco trovato per il codice AIC {aic.Trim()}." });

        SetClientCacheHeader();
        return Ok(farmaco);
    }

    private void SetClientCacheHeader() =>
        Response.Headers.CacheControl = $"public, max-age={ClientCacheSeconds}";
}
