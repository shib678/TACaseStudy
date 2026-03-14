using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RetailPricing.Application.Common.Interfaces;
using RetailPricing.Application.Features.PricingRecords.Commands.UpdatePricingRecord;
using RetailPricing.Domain.Entities;
using RetailPricing.Domain.Repositories;
using Xunit;

namespace RetailPricing.Application.Tests.Handlers;

public class UpdatePricingRecordCommandHandlerTests
{
    private readonly IPricingRecordRepository _repository  = Substitute.For<IPricingRecordRepository>();
    private readonly IUnitOfWork              _unitOfWork  = Substitute.For<IUnitOfWork>();
    private readonly UpdatePricingRecordCommandHandler _handler;

    public UpdatePricingRecordCommandHandlerTests()
    {
        _handler = new UpdatePricingRecordCommandHandler(
            _repository,
            _unitOfWork,
            NullLogger<UpdatePricingRecordCommandHandler>.Instance);
    }

    // ─────────────────────────────────────────────
    //  Record not found
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_RecordNotFound_ReturnsFail()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PricingRecord?)null);

        var result = await _handler.Handle(new UpdatePricingRecordCommand(id, 5.99m, "Milk", "user"), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains(id.ToString()));
    }

    [Fact]
    public async Task Handle_RecordNotFound_DoesNotCallSaveChanges()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PricingRecord?)null);

        await _handler.Handle(new UpdatePricingRecordCommand(Guid.NewGuid(), 5.99m, "Milk", "user"), default);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────
    //  Successful update
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_RecordFound_ReturnsOkResult()
    {
        var (record, cmd) = SetupFoundRecord();

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsUpdatedPrice()
    {
        var (_, cmd) = SetupFoundRecord(newPrice: 12.99m);

        var result = await _handler.Handle(cmd, default);

        result.Value.Price.Should().Be(12.99m);
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsUpdatedProductName()
    {
        var (_, cmd) = SetupFoundRecord(newProductName: "Organic Oat Milk");

        var result = await _handler.Handle(cmd, default);

        result.Value.ProductName.Should().Be("Organic Oat Milk");
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsCorrectStoreId()
    {
        var (record, cmd) = SetupFoundRecord();

        var result = await _handler.Handle(cmd, default);

        result.Value.StoreId.Should().Be(record.StoreId);
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsCorrectSku()
    {
        var (record, cmd) = SetupFoundRecord();

        var result = await _handler.Handle(cmd, default);

        result.Value.Sku.Should().Be(record.Sku);
    }

    [Fact]
    public async Task Handle_RecordFound_CallsRepositoryUpdate()
    {
        var (record, cmd) = SetupFoundRecord();

        await _handler.Handle(cmd, default);

        _repository.Received(1).Update(record);
    }

    [Fact]
    public async Task Handle_RecordFound_CallsSaveChangesOnce()
    {
        SetupFoundRecord();

        await _handler.Handle(
            new UpdatePricingRecordCommand(Guid.NewGuid(), 5.99m, "Milk", "user"),
            default);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsModifiedByFromCommand()
    {
        var (_, cmd) = SetupFoundRecord(modifiedBy: "analyst@store.com");

        var result = await _handler.Handle(cmd, default);

        result.Value.LastModifiedBy.Should().Be("analyst@store.com");
    }

    [Fact]
    public async Task Handle_RecordFound_ResultContainsLastModifiedAt_AfterNow()
    {
        var before        = DateTime.UtcNow.AddSeconds(-1);
        var (_, cmd)      = SetupFoundRecord();

        var result = await _handler.Handle(cmd, default);

        result.Value.LastModifiedAt.Should().BeAfter(before);
    }

    // ─────────────────────────────────────────────
    //  CancellationToken propagation
    // ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PricingRecord?)null);

        await _handler.Handle(
            new UpdatePricingRecordCommand(Guid.NewGuid(), 1m, "P", "u"),
            cts.Token);

        await _repository.Received().GetByIdAsync(Arg.Any<Guid>(), cts.Token);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private (PricingRecord record, UpdatePricingRecordCommand cmd) SetupFoundRecord(
        decimal newPrice       = 9.99m,
        string  newProductName = "Test Product",
        string  modifiedBy     = "user@store.com")
    {
        var id     = Guid.NewGuid();
        var record = PricingRecord.Create("AU-1001", "SKU-001", "Original Product", 1.00m,
                         new DateOnly(2024, 1, 15), "AUD", Guid.NewGuid());

        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(record);

        var cmd = new UpdatePricingRecordCommand(id, newPrice, newProductName, modifiedBy);
        return (record, cmd);
    }
}
