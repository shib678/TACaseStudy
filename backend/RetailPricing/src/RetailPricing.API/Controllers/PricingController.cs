using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;
using RetailPricing.Application.Features.PricingRecords.Queries.GetPricingRecordById;
using RetailPricing.Application.Features.PricingRecords.Queries.SearchPricingRecords;

namespace RetailPricing.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//[Authorize]
public class PricingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PricingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search pricing records with optional filters and pagination.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string? storeId,
        [FromQuery] string? sku,
        [FromQuery] string? productName,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchPricingRecordsQuery(
            storeId, sku, productName,
            minPrice, maxPrice,
            dateFrom, dateTo,
            pageNumber, pageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailed)
            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });

        var paged = result.Value;
        return Ok(new
        {
            items = paged.Items,
            pageNumber = paged.PageNumber,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount,
            totalPages = paged.TotalPages,
            hasPreviousPage = paged.HasPreviousPage,
            hasNextPage = paged.HasNextPage
        });
    }

    /// <summary>
    /// Get a single pricing record by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PricingRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPricingRecordByIdQuery(id), cancellationToken);

        if (result.IsFailed)
            return NotFound(new { message = result.Errors.First().Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a pricing record's price and/or product name.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdatePricingRecordResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePricingRecordRequest request,
        CancellationToken cancellationToken)
    {
        var modifiedBy = User.Identity?.Name ?? "unknown";

        var command = new UpdatePricingRecordCommand(
            id, request.Price, request.ProductName, modifiedBy);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailed)
        {
            var firstError = result.Errors.First().Message;
            if (firstError.Contains("not found"))
                return NotFound(new { message = firstError });

            return BadRequest(new { errors = result.Errors.Select(e => e.Message) });
        }

        return Ok(result.Value);
    }
}

public class UpdatePricingRecordRequest
{
    public decimal Price { get; set; }
    public string ProductName { get; set; } = string.Empty;
}
