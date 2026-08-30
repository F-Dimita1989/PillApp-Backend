using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PillApp.Api.Data;
using PillApp.Api.Dtos;
using PillApp.Api.Helpers;

namespace PillApp.Api.Services;

public class FarmaciReadService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FarmaciReadService> _logger;
    private readonly TimeSpan _cacheTtl;

    public FarmaciReadService(
        AppDbContext db,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<FarmaciReadService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;

        var minutes = configuration.GetValue<int?>("Cache:TtlMinutes") ?? 360;
        _cacheTtl = TimeSpan.FromMinutes(minutes);
    }

    public async Task<FarmacoSearchResultDto> SearchAsync(
        string q,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var term = q.Trim();
        var cacheKey = $"search:{term.ToLowerInvariant()}:{limit}:{offset}";

        if (_cache.TryGetValue(cacheKey, out FarmacoSearchResultDto? cached) && cached is not null)
            return cached;

        _logger.LogDebug("Cache miss per la ricerca {Term} (limit {Limit}, offset {Offset})", term, limit, offset);

        var query = FarmacoSearchQuery.Apply(
            _db.FarmaciClasseA.AsNoTracking(),
            term,
            useILike: _db.Database.IsNpgsql());

        var total = await query.CountAsync(cancellationToken);

        // Il tie-breaker su Aic rende l'ordinamento deterministico: senza di esso due
        // confezioni omonime possono cambiare posizione tra una pagina e la successiva.
        var items = await query
            .OrderBy(f => f.DenominazioneConfezione)
            .ThenBy(f => f.Aic)
            .Skip(offset)
            .Take(limit)
            .Select(FarmacoDtoMapper.ToLookupDto)
            .ToListAsync(cancellationToken);

        var result = new FarmacoSearchResultDto
        {
            Items = items,
            Total = total,
            Limit = limit,
            Offset = offset
        };

        Cache(cacheKey, result);
        return result;
    }

    public async Task<FarmacoLookupDto?> GetByAicAsync(string aic, CancellationToken cancellationToken)
    {
        var normalizedAic = aic.Trim();
        var cacheKey = $"aic:{normalizedAic}";

        if (_cache.TryGetValue(cacheKey, out FarmacoLookupDto? cached))
            return cached;

        _logger.LogDebug("Cache miss per il codice AIC {Aic}", normalizedAic);

        var farmaco = await _db.FarmaciClasseA
            .AsNoTracking()
            .Where(f => f.Aic == normalizedAic)
            .Select(FarmacoDtoMapper.ToLookupDto)
            .FirstOrDefaultAsync(cancellationToken);

        Cache(cacheKey, farmaco);
        return farmaco;
    }

    private void Cache<T>(string key, T value) =>
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheTtl,
            Size = 1
        });
}
