using System.ComponentModel.DataAnnotations;
using Ehgiz.Application.DTOs.Tools;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class ToolServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private readonly ISavedSearchService _savedSearches = Substitute.For<ISavedSearchService>();
    private ToolService _sut = null!;
    private ApplicationUser _owner = null!;
    private ApplicationUser _stranger = null!;
    private Category _category = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new ToolService(
            _db.Uow,
            _cloudinary,
            _savedSearches,
            NullLogger<ToolService>.Instance);

        _owner = await _db.SeedUserAsync(fullName: "Owner");
        _stranger = await _db.SeedUserAsync(fullName: "Stranger");
        _category = await _db.SeedCategoryAsync();
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private static IFormFile FakeImage(string name = "photo.jpg")
    {
        var bytes = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, name, name);
    }

    // ── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_FiltersByCategoryPriceAndSearchTerm()
    {
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Bosch Drill", pricePerDay: 10m);
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Makita Saw", pricePerDay: 50m);
        var otherCategory = await _db.SeedCategoryAsync("Garden");
        await _db.SeedToolAsync(_owner.Id, otherCategory.Id, name: "Drill Press", pricePerDay: 10m);

        var result = await _sut.GetAllAsync(new ToolFilterDto
        {
            CategoryId = _category.Id,
            SearchTerm = "Drill",
            MaxPrice = 20m
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Bosch Drill", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task GetAllAsync_PaginatesNewestFirst()
    {
        for (var i = 1; i <= 5; i++)
        {
            var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: $"Tool {i}");
            tool.CreatedAt = DateTime.UtcNow.AddMinutes(i);
        }
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var page2 = await _sut.GetAllAsync(new ToolFilterDto { Page = 2, PageSize = 2 });

        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal("Tool 3", page2.Items[0].Name);
        Assert.Equal("Tool 2", page2.Items[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_GeoSearchFiltersByRadiusAndComputesDistance()
    {
        // Cairo downtown vs. ~2.5km away vs. Alexandria (~180km away)
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Near Tool", latitude: 30.05, longitude: 31.24);
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Far Tool", latitude: 31.20, longitude: 29.92);
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "No Coords Tool");

        var result = await _sut.GetAllAsync(new ToolFilterDto
        {
            NearLat = 30.0444,
            NearLng = 31.2357,
            RadiusKm = 10
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("Near Tool", item.Name);
        Assert.NotNull(item.DistanceKm);
        Assert.InRange(item.DistanceKm!.Value, 0, 10);
    }

    [Fact]
    public async Task GetAllAsync_GeoSearchOrdersByProximity()
    {
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Far Tool", latitude: 31.20, longitude: 29.92);
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Near Tool", latitude: 30.05, longitude: 31.24);

        var result = await _sut.GetAllAsync(new ToolFilterDto { NearLat = 30.0444, NearLng = 31.2357 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Near Tool", result.Items[0].Name);
        Assert.True(result.Items[0].DistanceKm < result.Items[1].DistanceKm);
    }

    // ── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(new CreateToolDto { CategoryId = 999, Name = "X" }, _owner.Id));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenOnlyOneCoordinateProvided()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateToolDto
            {
                CategoryId = _category.Id,
                Name = "X",
                Latitude = 30.0
            }, _owner.Id));
    }

    [Fact]
    public async Task CreateAsync_PersistsToolAndTriggersSavedSearchNotify()
    {
        var result = await _sut.CreateAsync(new CreateToolDto
        {
            CategoryId = _category.Id,
            Name = "New Drill",
            PricePerDay = 15m,
            Condition = ToolCondition.New
        }, _owner.Id);

        Assert.Equal("New Drill", result.Name);
        Assert.Equal(_owner.Id, result.OwnerId);
        Assert.True(result.IsAvailable);
        Assert.Equal(nameof(ToolCondition.New), result.Condition);

        await _savedSearches.Received(1).NotifyMatchesAsync(result.Id);
    }

    [Fact]
    public async Task CreateAsync_SucceedsEvenIfSavedSearchNotifyFails()
    {
        _savedSearches.NotifyMatchesAsync(Arg.Any<int>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var result = await _sut.CreateAsync(new CreateToolDto
        {
            CategoryId = _category.Id,
            Name = "Resilient Tool",
            PricePerDay = 10m
        }, _owner.Id);

        Assert.True(result.Id > 0);
    }

    // ── UpdateAsync / DeleteAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotOwner()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateAsync(tool.Id, new UpdateToolDto { CategoryId = _category.Id, Name = "Hacked" }, _stranger.Id));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Old Name");

        var result = await _sut.UpdateAsync(tool.Id, new UpdateToolDto
        {
            CategoryId = _category.Id,
            Name = "New Name",
            PricePerDay = 25m,
            IsAvailable = false
        }, _owner.Id);

        Assert.Equal("New Name", result.Name);
        Assert.Equal(25m, result.PricePerDay);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotOwner()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteAsync(tool.Id, _stranger.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTool()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await _sut.DeleteAsync(tool.Id, _owner.Id);

        Assert.Empty(_db.Context.Tools.Where(t => t.Id == tool.Id).ToList());
    }

    [Fact]
    public async Task DeleteAsync_BlocksWhenToolHasActiveBookings()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);
        await _db.SeedBookingAsync(tool.Id, _stranger.Id, BookingStatus.Active);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(tool.Id, _owner.Id));
        Assert.Single(_db.Context.Tools.Where(t => t.Id == tool.Id).ToList());
    }

    [Fact]
    public async Task CreateAsync_RejectsNonPositivePrice()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(new CreateToolDto
        {
            CategoryId = _category.Id,
            Name = "Free Money Glitch",
            PricePerDay = -10m
        }, _owner.Id));
    }

    [Fact]
    public async Task UpdateAsync_RejectsNegativeInsurance()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.UpdateAsync(tool.Id, new UpdateToolDto
        {
            CategoryId = _category.Id,
            Name = "Drill",
            PricePerDay = 10m,
            InsurancePrice = -5m
        }, _owner.Id));
    }

    // ── Images ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImagesAsync_FirstImageBecomesPrimary()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);
        var uploads = 0;
        _cloudinary.UploadImageAsync(Arg.Any<IFormFile>())
            .Returns(_ => new ImageUploadResult { ImageUrl = $"https://img/{++uploads}", PublicId = $"pub{uploads}" });

        var urls = await _sut.UploadImagesAsync(tool.Id, [FakeImage("a.jpg"), FakeImage("b.jpg")], _owner.Id);

        Assert.Equal(2, urls.Count);
        var images = _db.Context.ToolImages.Where(i => i.ToolId == tool.Id).OrderBy(i => i.Id).ToList();
        Assert.True(images[0].IsPrimary);
        Assert.False(images[1].IsPrimary);
    }

    [Fact]
    public async Task UploadImagesAsync_ThrowsWhenNoImagesProvided()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.UploadImagesAsync(tool.Id, [], _owner.Id));
    }

    [Fact]
    public async Task UploadImagesAsync_ThrowsWhenNotOwner()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UploadImagesAsync(tool.Id, [FakeImage()], _stranger.Id));
    }

    [Fact]
    public async Task DeleteImageAsync_PromotesNextImageToPrimary()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);
        _db.Context.ToolImages.AddRange(
            new ToolImage { ToolId = tool.Id, ImageUrl = "https://img/1", PublicId = "pub1", IsPrimary = true },
            new ToolImage { ToolId = tool.Id, ImageUrl = "https://img/2", PublicId = "pub2", IsPrimary = false });
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var primaryId = _db.Context.ToolImages.Single(i => i.IsPrimary).Id;
        // Mirror production: the service runs on a fresh request-scoped context.
        _db.Context.ChangeTracker.Clear();

        await _sut.DeleteImageAsync(primaryId, _owner.Id);

        await _cloudinary.Received(1).DeleteImageAsync("pub1");
        var remaining = Assert.Single(_db.Context.ToolImages.AsNoTracking().Where(i => i.ToolId == tool.Id).ToList());
        Assert.True(remaining.IsPrimary);
    }

    [Fact]
    public async Task SetPrimaryImageAsync_MakesExactlyOneImagePrimary()
    {
        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id);
        _db.Context.ToolImages.AddRange(
            new ToolImage { ToolId = tool.Id, ImageUrl = "https://img/1", IsPrimary = true },
            new ToolImage { ToolId = tool.Id, ImageUrl = "https://img/2", IsPrimary = false });
        await _db.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var secondId = _db.Context.ToolImages.Single(i => i.ImageUrl == "https://img/2").Id;
        // Mirror production: the service runs on a fresh request-scoped context.
        _db.Context.ChangeTracker.Clear();

        await _sut.SetPrimaryImageAsync(secondId, _owner.Id);

        var images = _db.Context.ToolImages.AsNoTracking().Where(i => i.ToolId == tool.Id).ToList();
        Assert.Equal(secondId, Assert.Single(images, i => i.IsPrimary).Id);
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyOwnersTools()
    {
        await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Mine");
        await _db.SeedToolAsync(_stranger.Id, _category.Id, name: "Theirs");

        var result = await _sut.GetByOwnerAsync(_owner.Id);

        Assert.Equal("Mine", Assert.Single(result).Name);
    }
}
