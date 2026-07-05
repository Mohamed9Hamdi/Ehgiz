using Ehgiz.Application.DTOs.Profile;
using Ehgiz.Application.Interfaces;
using Ehgiz.Application.Services;
using Ehgiz.DAL.Entities;
using Ehgiz.Tests.TestHelpers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Ehgiz.Tests.Services;

public class ProfileServiceTests : IAsyncLifetime
{
    private readonly TestDb _db = new();
    private readonly ICloudinaryService _cloudinary = Substitute.For<ICloudinaryService>();
    private ProfileService _sut = null!;
    private ApplicationUser _user = null!;

    public async ValueTask InitializeAsync()
    {
        _sut = new ProfileService(_db.UserManager, _cloudinary, new Mapper(TypeAdapterConfig.GlobalSettings));
        _user = await _db.CreateIdentityUserAsync("profile@test.local");
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();

    private static IFormFile FakeImage()
    {
        var bytes = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "image.jpg");
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfileWithRoles()
    {
        var result = await _sut.GetProfileAsync(_user.Id);

        Assert.NotNull(result);
        Assert.Equal("profile@test.local", result!.Email);
        Assert.Contains("user", result.Roles);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullForUnknownUser()
    {
        Assert.Null(await _sut.GetProfileAsync(999999));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullForDeactivatedUser()
    {
        _user.IsActive = false;
        await _db.UserManager.UpdateAsync(_user);

        Assert.Null(await _sut.GetProfileAsync(_user.Id));
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsAndPersistsProvidedFields()
    {
        var result = await _sut.UpdateProfileAsync(_user.Id,
            new UpdateProfileDTO("  New Name  ", " 0111 ", null, "  Giza "));

        Assert.NotNull(result);
        Assert.Equal("New Name", result!.FullName);
        Assert.Equal("0111", result.PhoneNumber);
        Assert.Equal("Giza", result.City);

        var stored = await _db.UserManager.FindByIdAsync(_user.Id.ToString());
        Assert.Equal("New Name", stored!.FullName);
    }

    [Fact]
    public async Task UpdateProfileAsync_IgnoresBlankFullName()
    {
        var original = _user.FullName;

        var result = await _sut.UpdateProfileAsync(_user.Id, new UpdateProfileDTO("   ", null, null, null));

        Assert.Equal(original, result!.FullName);
    }

    [Fact]
    public async Task UpdateProfileImageAsync_UploadsAndReplacesOldImage()
    {
        _user.ProfileImagePublicId = "old-public-id";
        _user.ProfileImageUrl = "https://img/old";
        await _db.UserManager.UpdateAsync(_user);

        _cloudinary.UploadImageAsync(Arg.Any<IFormFile>())
            .Returns(new ImageUploadResult { ImageUrl = "https://img/new", PublicId = "new-public-id" });

        var result = await _sut.UpdateProfileImageAsync(_user.Id, FakeImage());

        Assert.Equal("https://img/new", result!.ProfileImageUrl);
        await _cloudinary.Received(1).DeleteImageAsync("old-public-id");

        var stored = await _db.UserManager.FindByIdAsync(_user.Id.ToString());
        Assert.Equal("new-public-id", stored!.ProfileImagePublicId);
    }

    [Fact]
    public async Task RemoveProfileImageAsync_DeletesFromCloudinaryAndClearsFields()
    {
        _user.ProfileImagePublicId = "pid";
        _user.ProfileImageUrl = "https://img/x";
        await _db.UserManager.UpdateAsync(_user);

        var result = await _sut.RemoveProfileImageAsync(_user.Id);

        Assert.Null(result!.ProfileImageUrl);
        await _cloudinary.Received(1).DeleteImageAsync("pid");

        var stored = await _db.UserManager.FindByIdAsync(_user.Id.ToString());
        Assert.Null(stored!.ProfileImagePublicId);
    }

    [Fact]
    public async Task RemoveProfileImageAsync_SkipsCloudinaryWhenNoImage()
    {
        await _sut.RemoveProfileImageAsync(_user.Id);

        await _cloudinary.DidNotReceiveWithAnyArgs().DeleteImageAsync(default!);
    }
}
