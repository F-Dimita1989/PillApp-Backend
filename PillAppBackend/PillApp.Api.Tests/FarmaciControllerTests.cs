using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PillApp.Api.Controllers;
using PillApp.Api.Data;
using PillApp.Api.Dtos;
using PillApp.Api.Models;
using PillApp.Api.Services;

namespace PillApp.Api.Tests;

public class FarmaciControllerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        var now = DateTimeOffset.UtcNow;
        for (var i = 1; i <= 25; i++)
        {
            context.FarmaciClasseA.Add(new FarmacoClasseA
            {
                Id = i,
                Aic = $"0230760{i:D2}",
                DenominazioneConfezione = $"Paracetamol {i}mg",
                PrincipioAttivo = "Paracetamolo",
                DescrizioneGruppo = "Analgesici",
                PrezzoPubblico = 5.50m + i,
                TitolareAic = "Test Pharma",
                CodiceGruppoEquivalenza = "001",
                InListaTrasparenzaAifa = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        context.SaveChanges();
        return context;
    }

    private static FarmaciReadService CreateService(AppDbContext context) =>
        new(context,
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 64 }),
            new ConfigurationBuilder().Build(),
            NullLogger<FarmaciReadService>.Instance);

    private static FarmaciController CreateController(AppDbContext context) =>
        new(CreateService(context))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task GetByAic_WithValidAic_ReturnsDrug()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.GetByAic("023076001");
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal(200, okResult.StatusCode);
        var drug = Assert.IsType<FarmacoLookupDto>(okResult.Value);
        Assert.Equal("Analgesici", drug.DescrizioneGruppo);
    }

    [Fact]
    public async Task GetByAic_SetsCacheControlHeader()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        await controller.GetByAic("023076001");

        Assert.Contains("max-age", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetByAic_WithInvalidAic_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.GetByAic("999999999");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByAic_WithEmptyAic_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.GetByAic("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithTooShortQuery_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("pa");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithValidQuery_ReturnsPaginatedResults()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("Paracetamolo", limit: 10, offset: 0);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<FarmacoSearchResultDto>(okResult.Value);

        Assert.Equal(25, body.Total);
        Assert.Equal(10, body.Items.Count);
        Assert.Equal(10, body.Limit);
    }

    [Fact]
    public async Task Search_WithOffset_ReturnsNextPage()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("Paracetamolo", limit: 10, offset: 20);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<FarmacoSearchResultDto>(okResult.Value);

        Assert.Equal(25, body.Total);
        Assert.Equal(5, body.Items.Count);
        Assert.Equal(20, body.Offset);
    }

    [Fact]
    public async Task Search_WithInvalidLimit_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("Paracetamolo", limit: 500, offset: 0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithNegativeOffset_ReturnsBadRequest()
    {
        using var context = CreateDbContext();
        var controller = CreateController(context);

        var result = await controller.Search("Paracetamolo", limit: 10, offset: -1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Search_WithDuplicateNames_PaginatesWithoutOverlap()
    {
        using var context = CreateDbContext();

        var now = DateTimeOffset.UtcNow;
        for (var i = 1; i <= 6; i++)
        {
            context.FarmaciClasseA.Add(new FarmacoClasseA
            {
                Id = 100 + i,
                Aic = $"0999999{i:D2}",
                DenominazioneConfezione = "Confezione Omonima",
                PrincipioAttivo = "Ibuprofene",
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var firstPage = await controller.Search("Omonima", limit: 3, offset: 0);
        var secondPage = await controller.Search("Omonima", limit: 3, offset: 3);

        var firstItems = Assert.IsType<FarmacoSearchResultDto>(
            Assert.IsType<OkObjectResult>(firstPage.Result).Value).Items;
        var secondItems = Assert.IsType<FarmacoSearchResultDto>(
            Assert.IsType<OkObjectResult>(secondPage.Result).Value).Items;

        var allAic = firstItems.Concat(secondItems).Select(f => f.Aic).ToList();

        Assert.Equal(6, allAic.Count);
        Assert.Equal(6, allAic.Distinct().Count());
    }

    [Fact]
    public async Task GetByAic_SecondCall_IsServedFromCache()
    {
        using var context = CreateDbContext();
        var service = CreateService(context);

        var first = await service.GetByAicAsync("023076001", CancellationToken.None);
        Assert.NotNull(first);

        context.FarmaciClasseA.RemoveRange(context.FarmaciClasseA);
        await context.SaveChangesAsync();

        var second = await service.GetByAicAsync("023076001", CancellationToken.None);

        Assert.NotNull(second);
        Assert.Equal(first.Aic, second.Aic);
    }
}
