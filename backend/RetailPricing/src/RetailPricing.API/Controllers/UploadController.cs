using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailPricing.Application.Features.PricingRecords.Commands.UploadPricingFeed;
using RetailPricing.Domain.Repositories;

namespace RetailPricing.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//[Authorize]
public class UploadController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUploadBatchRepository _batchRepository;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IMediator mediator,
        IUploadBatchRepository batchRepository,
        ILogger<UploadController> logger)
    {
        _mediator = mediator;
        _batchRepository = batchRepository;
        _logger = logger;
    }

    /// <summary>
    /// Upload a CSV pricing feed for a specific store.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10_485_760)] // 10 MB
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadPricingFeedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPricingFeed(
        IFormFile file,
        [FromQuery] string storeId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file was uploaded." });

        var uploadedBy = User.Identity?.Name ?? "unknown";

        var command = new UploadPricingFeedCommand(
            FileName: file.FileName,
            CsvStream: file.OpenReadStream(),
            StoreId: storeId,
            UploadedBy: uploadedBy
        );

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get the status and history of upload batches for a store.
    /// </summary>
    [HttpGet("history/{storeId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUploadHistory(
        string storeId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var batches = await _batchRepository.GetByStoreIdAsync(storeId, take, cancellationToken);

        var response = batches.Select(b => new
        {
            b.Id,
            b.FileName,
            b.StoreId,
            b.TotalRows,
            b.ProcessedRows,
            b.FailedRows,
            Status = b.Status.ToString(),
            b.ErrorSummary,
            b.CreatedAt,
            b.CreatedBy
        });

        return Ok(response);
    }
}
