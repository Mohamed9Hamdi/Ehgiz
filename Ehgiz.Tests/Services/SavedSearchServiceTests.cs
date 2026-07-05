using System.ComponentModel.DataAnnotations;
using Ehgiz.Application.DTOs.Notifications;
using Ehgiz.Application.DTOs.SavedSearches;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.DAL.Enums;
using Ehgiz.Tests.TestHelpers;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class SavedSearchServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private SavedSearchService _sut = null!;
    private ApplicationUser _user = null!;
    private ApplicationUser _owner = null!;
    private Category _category = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new SavedSearchService(_db.Uow, _notifications);
        _user = await _db.SeedUserAsync();
        _owner = await _db.SeedUserAsync();
        _category = await _db.SeedCategoryAsync();
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    // ── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ThrowsWhenNoCriteriaProvided()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "   " }));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenMinPriceExceedsMaxPrice()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { MinPrice = 50, MaxPrice = 10 }));
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenCategoryDoesNotExist()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { CategoryId = 999 }));
    }

    [Fact]
    public async Task CreateAsync_TrimsTextAndReturnsDtoWithCategoryName()
    {
        var result = await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto
        {
            SearchTerm = "  drill  ",
            Location = "  Cairo  ",
            CategoryId = _category.Id,
            MinPrice = 5,
            MaxPrice = 50,
            Condition = ToolCondition.Good
        });

        Assert.Equal("drill", result.SearchTerm);
        Assert.Equal("Cairo", result.Location);
        Assert.Equal(_category.Name, result.CategoryName);
        Assert.Equal(nameof(ToolCondition.Good), result.Condition);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetAllForUserAsync_ReturnsOnlyOwnSearches()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill" });
        await _sut.CreateAsync(_owner.Id, new CreateSavedSearchDto { SearchTerm = "saw" });

        var result = await _sut.GetAllForUserAsync(_user.Id);

        Assert.Equal("drill", Assert.Single(result).SearchTerm);
    }

    // ── DeleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesOwnSearch()
    {
        var created = await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill" });

        await _sut.DeleteAsync(created.Id, _user.Id);

        Assert.Empty(await _sut.GetAllForUserAsync(_user.Id));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotOwner()
    {
        var created = await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill" });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(created.Id, _owner.Id));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(12345, _user.Id));
    }

    // ── NotifyMatchesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task NotifyMatchesAsync_NotifiesUserWhoseCriteriaMatch()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto
        {
            SearchTerm = "drill",
            CategoryId = _category.Id,
            MinPrice = 5,
            MaxPrice = 20,
            Condition = ToolCondition.Good
        });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id,
            name: "Cordless Drill", pricePerDay: 10m, condition: ToolCondition.Good);

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n =>
            n.UserId == _user.Id &&
            n.Type == NotificationType.SavedSearchMatch &&
            n.Url == $"/tools/{tool.Id}"));
    }

    [Fact]
    public async Task NotifyMatchesAsync_SkipsToolOwnerOwnSearches()
    {
        await _sut.CreateAsync(_owner.Id, new CreateSavedSearchDto { SearchTerm = "drill" });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Cordless Drill");

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task NotifyMatchesAsync_SkipsWhenPriceOutOfRange()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill", MaxPrice = 5 });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Cordless Drill", pricePerDay: 10m);

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task NotifyMatchesAsync_MatchesSearchTermInDescriptionCaseInsensitive()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "BOSCH" });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id,
            name: "Hammer", description: "A genuine bosch hammer drill");

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n => n.UserId == _user.Id));
    }

    [Fact]
    public async Task NotifyMatchesAsync_SkipsWhenLocationDoesNotMatch()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill", Location = "Alexandria" });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Drill", location: "Cairo");

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task NotifyMatchesAsync_NoopWhenToolMissing()
    {
        await _sut.NotifyMatchesAsync(999);

        await _notifications.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task NotifyMatchesAsync_NotifiesUserOnceEvenWithMultipleMatchingSearches()
    {
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { SearchTerm = "drill" });
        await _sut.CreateAsync(_user.Id, new CreateSavedSearchDto { CategoryId = _category.Id });

        var tool = await _db.SeedToolAsync(_owner.Id, _category.Id, name: "Drill");

        await _sut.NotifyMatchesAsync(tool.Id);

        await _notifications.Received(1).CreateAsync(Arg.Is<CreateNotificationDto>(n => n.UserId == _user.Id));
    }
}
