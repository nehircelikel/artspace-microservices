using System.Security.Claims;
using ArtService.API.Controllers;
using ArtService.API.DTOs;
using ArtService.Core.Entities;
using ArtService.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArtService.Tests.Unit;

public class ArtworkControllerTests
{
    private readonly Mock<IArtworkRepository> _repo = new();

    private ArtworkController BuildController(Guid userId, string username = "artist")
    {
        var controller = new ArtworkController(_repo.Object);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
        }, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }

    [Fact]
    public async Task Create_stamps_artist_identity_from_claims()
    {
        var artistId = Guid.NewGuid();
        var controller = BuildController(artistId, "picasso");
        Artwork? saved = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<Artwork>()))
            .Callback<Artwork>(a => saved = a)
            .ReturnsAsync((Artwork a) => a);

        await controller.Create(new CreateArtworkDto { Title = "T", Category = "Painting" });

        Assert.NotNull(saved);
        Assert.Equal(artistId, saved!.ArtistId);
        Assert.Equal("picasso", saved.ArtistUsername);
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Artwork { Id = Guid.NewGuid(), ArtistId = Guid.NewGuid() });

        var result = await controller.Update(Guid.NewGuid(), new UpdateArtworkDto { Title = "x" });

        Assert.IsType<ForbidResult>(result.Result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Artwork>()), Times.Never);
    }

    [Fact]
    public async Task Delete_missing_artwork_returns_NotFound()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Artwork?)null);

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_by_non_owner_is_forbidden()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Artwork { Id = Guid.NewGuid(), ArtistId = Guid.NewGuid() });

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
