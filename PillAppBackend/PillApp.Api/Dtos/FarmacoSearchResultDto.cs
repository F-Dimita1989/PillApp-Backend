namespace PillApp.Api.Dtos;

public class FarmacoSearchResultDto
{
    public IReadOnlyList<FarmacoLookupDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}
