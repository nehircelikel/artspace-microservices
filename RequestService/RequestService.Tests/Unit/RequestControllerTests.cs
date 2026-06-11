using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RequestService.API.Controllers;
using RequestService.API.DTOs;
using RequestService.Core.Entities;
using RequestService.Core.Interfaces;
using RequestService.Infrastructure.Services;
using System.Security.Claims;

namespace RequestService.Tests.Unit;

public class RequestControllerTests
{
    private readonly Mock<IArtworkRequestRepository> _repo = new();
    private readonly Mock<IRabbitMQPublisher> _publisher = new();

    private RequestController BuildController(Guid userId, string username = "tester", string email = "t@example.com")
    {
        var controller = new RequestController(_repo.Object, _publisher.Object);
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
        }, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
        return controller;
    }

    private static ArtworkRequest Pending(Guid artistId, Guid requesterId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Commission",
        Status = RequestStatus.Pending,
        ArtistId = artistId,
        ArtistUsername = "artist",
        RequesterId = requesterId,
        RequesterUsername = "client",
    };

    [Fact]
    public async Task Create_rejects_request_to_self()
    {
        var me = Guid.NewGuid();
        var controller = BuildController(me);

        var result = await controller.Create(new CreateRequestDto { Title = "X", ArtistId = me, ArtistUsername = "me" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _repo.Verify(r => r.CreateAsync(It.IsAny<ArtworkRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_persists_request_and_notifies_artist()
    {
        var me = Guid.NewGuid();
        var artist = Guid.NewGuid();
        var controller = BuildController(me, "client", "client@example.com");

        ArtworkRequest? saved = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<ArtworkRequest>()))
            .Callback<ArtworkRequest>(r => saved = r)
            .ReturnsAsync((ArtworkRequest r) => r);
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => saved);

        var result = await controller.Create(new CreateRequestDto
        {
            Title = "Paint my cat",
            ArtistId = artist,
            ArtistUsername = "artist",
        });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(saved);
        Assert.Equal(RequestStatus.Pending, saved!.Status);
        Assert.Equal(me, saved.RequesterId);
        Assert.Equal("client@example.com", saved.RequesterEmail);
        _publisher.Verify(p => p.PublishNotification(artist, It.Is<string>(s => s.Contains("Paint my cat"))), Times.Once);
    }

    [Fact]
    public async Task Accept_requires_estimated_cost_and_time()
    {
        var artist = Guid.NewGuid();
        var request = Pending(artist, Guid.NewGuid()); // no estimates set
        _repo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var controller = BuildController(artist, "artist");
        var result = await controller.Accept(request.Id);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<ArtworkRequest>()), Times.Never);
    }

    [Fact]
    public async Task Accept_transitions_to_accepted_when_estimates_present()
    {
        var artist = Guid.NewGuid();
        var request = Pending(artist, Guid.NewGuid());
        request.EstimatedCost = 200m;
        request.EstimatedTime = "2 weeks";
        _repo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var controller = BuildController(artist, "artist");
        var result = await controller.Accept(request.Id);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(RequestStatus.Accepted, request.Status);
        _publisher.Verify(p => p.PublishNotification(request.RequesterId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Withdraw_is_forbidden_for_non_requester()
    {
        var request = Pending(Guid.NewGuid(), Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var controller = BuildController(Guid.NewGuid()); // a stranger
        var result = await controller.Withdraw(request.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetById_is_forbidden_for_non_participant()
    {
        var request = Pending(Guid.NewGuid(), Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var controller = BuildController(Guid.NewGuid());
        var result = await controller.GetById(request.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
