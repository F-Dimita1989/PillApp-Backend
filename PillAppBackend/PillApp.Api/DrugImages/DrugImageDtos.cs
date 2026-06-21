namespace PillApp.Api.DrugImages;

public sealed class DrugImageAnalyzeRequestDto
{
    public string? ImageBase64 { get; set; }

    public string? ImageUrl { get; set; }
}

public sealed class DrugImageAnalyzeResponseDto
{
    public string? Aic { get; set; }

    public string? DrugName { get; set; }

    public string? RawResponse { get; set; }

    public double? Confidence { get; set; }
}

public sealed class DrugImageServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 25;
}