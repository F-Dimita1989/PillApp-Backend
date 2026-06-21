using Microsoft.AspNetCore.Mvc;

namespace PillApp.Api.DrugImages;

[ApiController]
[Route("api/[controller]")]
public class DrugImageController : ControllerBase
{
    private readonly IDrugImageService _drugImageService;

    public DrugImageController(IDrugImageService drugImageService)
    {
        _drugImageService = drugImageService;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DrugImageAnalyzeResponseDto>> Analyze([FromBody] DrugImageAnalyzeRequestDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body missing.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageBase64) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest("Provide either ImageBase64 or ImageUrl.");
        }

        var result = await _drugImageService.AnalyzeAsync(request, cancellationToken);

        return Ok(result);
    }
}