using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PillApp.Api.Controllers;
using PillApp.Api.Data;
using PillApp.Api.Models;

namespace PillApp.Api.Tests;

public class FarmaciControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        // Seed test data
        context.FarmaciClasseA.Add(new FarmacoClasseA
        {
            Id = 1,
            Aic = "023076010",
            DenominazioneConfezione = "Paracetamol 500mg",
            PrincipioAttivo = "Paracetamolo",
            PrezzoPubblico = 5.50m,
            TitolareAic = "Test Pharma",
            CodiceGruppoEquivalenza = "001",
            InListaTrasparenzaAifa = true,
            SoloListaRegione = null,
            MetriCubiOssigeno = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetByAic_WithValidAic_ReturnsDrug()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new FarmaciController(context);

        // Act
        var result = await controller.GetByAic("023076010");
        var okResult = result.Result as OkObjectResult;

        // Assert
        Assert.NotNull(okResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetByAic_WithInvalidAic_ReturnsNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new FarmaciController(context);

        // Act
        var result = await controller.GetByAic("999999999");
        var notFoundResult = result.Result as NotFoundObjectResult;

        // Assert
        Assert.NotNull(notFoundResult);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetByAic_WithEmptyAic_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new FarmaciController(context);

        // Act
        var result = await controller.GetByAic("");
        var badResult = result.Result as BadRequestObjectResult;

        // Assert
        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
    }

    [Fact]
    public async Task Search_WithEmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new FarmaciController(context);

        // Act
        var result = await controller.Search("");
        var badResult = result.Result as BadRequestObjectResult;

        // Assert
        Assert.NotNull(badResult);
        Assert.Equal(400, badResult.StatusCode);
    }
}
