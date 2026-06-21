using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace PillApp.Api.DrugImages;

public interface IDrugImageService
{
    Task<DrugImageAnalyzeResponseDto> AnalyzeAsync(DrugImageAnalyzeRequestDto request, CancellationToken cancellationToken = default);
}

public static class DrugImageServiceCollectionExtensions
{
    public static IServiceCollection AddDrugImageService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DrugImageServiceOptions>(configuration.GetSection("DrugImageService"));

        services.AddHttpClient<IDrugImageService, DrugImageService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DrugImageServiceOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                throw new InvalidOperationException("Missing DrugImageService configuration. Set DrugImageService__BaseUrl.");
            }

            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 25);
        });

        return services;
    }
}

internal sealed class DrugImageService : IDrugImageService
{
    private readonly HttpClient _httpClient;

    public DrugImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DrugImageAnalyzeResponseDto> AnalyzeAsync(DrugImageAnalyzeRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new ArgumentException("Provide either ImageBase64 or ImageUrl.", nameof(request));
        }

        using var response = await _httpClient.PostAsJsonAsync("analyze", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Drug image service call failed with status {(int)response.StatusCode}: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<DrugImageAnalyzeResponseDto>(cancellationToken: cancellationToken);

        return payload ?? throw new InvalidOperationException("Drug image service returned an empty response.");
    }
}