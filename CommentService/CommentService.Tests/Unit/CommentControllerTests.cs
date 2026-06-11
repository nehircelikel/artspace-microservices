using System.Security.Claims;
using CommentService.API.Controllers;
using CommentService.API.DTOs;
using CommentService.Core.Entities;
using CommentService.Core.Interfaces;
using CommentService.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CommentService.Tests.Unit;

public class CommentControllerTests
{
    private readonly Mock<ICommentRepository> _repo = new();
    private readonly Mock<IRabbitMQPublisher> _publisher = new();

    private CommentController BuildController(Guid userId, string username = "tester")
    {
        var controller = new CommentController(_repo.Object, _publisher.Object);
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

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(null)]
    public async Task Create_review_with_invalid_rating_returns_BadRequest(int? rating)
    {
        var controller = BuildController(Guid.NewGuid());
        var dto = new CreateCommentDto { Content = "x", Rating = rating, ArtworkId = Guid.NewGuid(), ArtistId = Guid.NewGuid() };

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Create_review_by_the_artwork_artist_is_forbidden()
    {
        var artistId = Guid.NewGuid();
        var controller = BuildController(artistId);
        var dto = new CreateCommentDto { Content = "x", Rating = 5, ArtworkId = Guid.NewGuid(), ArtistId = artistId };

        var result = await controller.Create(dto);

        Assert.IsType<ForbidResult>(result.Result);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Create_reply_with_missing_parent_returns_NotFound()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Comment?)null);
        var dto = new CreateCommentDto { Content = "r", ArtworkId = Guid.NewGuid(), ArtistId = Guid.NewGuid(), ParentId = Guid.NewGuid() };

        var result = await controller.Create(dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_valid_reply_has_null_rating_and_publishes()
    {
        var controller = BuildController(Guid.NewGuid());
        // Parent authored by someone other than the replier, so a notification is due.
        var parent = new Comment { Id = Guid.NewGuid(), ArtworkId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _repo.Setup(r => r.GetByIdAsync(parent.Id)).ReturnsAsync(parent);
        Comment? saved = null;
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => saved = c)
            .ReturnsAsync((Comment c) => c);

        var dto = new CreateCommentDto { Content = "r", Rating = 5, ArtworkId = parent.ArtworkId, ArtistId = Guid.NewGuid(), ParentId = parent.Id };
        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.NotNull(saved);
        Assert.Null(saved!.Rating);                  // reply ignores any rating
        Assert.Equal(parent.Id, saved.ParentId);
        _publisher.Verify(p => p.Publish(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Reply_to_your_own_comment_does_not_notify_yourself()
    {
        var userId = Guid.NewGuid();
        var controller = BuildController(userId);
        // Replying to a comment you authored: no self-notification should be published.
        var parent = new Comment { Id = Guid.NewGuid(), ArtworkId = Guid.NewGuid(), UserId = userId };
        _repo.Setup(r => r.GetByIdAsync(parent.Id)).ReturnsAsync(parent);
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

        var dto = new CreateCommentDto { Content = "r", ArtworkId = parent.ArtworkId, ArtistId = Guid.NewGuid(), ParentId = parent.Id };
        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _publisher.Verify(p => p.Publish(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Create_review_publishes_a_notification_for_the_artwork_artist()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.CreateAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

        var dto = new CreateCommentDto { Content = "great", Rating = 5, ArtworkId = Guid.NewGuid(), ArtistId = Guid.NewGuid() };
        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _publisher.Verify(p => p.Publish(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Create_second_review_by_same_user_returns_Conflict()
    {
        var userId = Guid.NewGuid();
        var artworkId = Guid.NewGuid();
        var controller = BuildController(userId);
        _repo.Setup(r => r.HasUserReviewedAsync(artworkId, userId)).ReturnsAsync(true);

        var dto = new CreateCommentDto { Content = "again", Rating = 4, ArtworkId = artworkId, ArtistId = Guid.NewGuid() };
        var result = await controller.Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
        _repo.Verify(r => r.CreateAsync(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Update_by_non_owner_is_forbidden()
    {
        var controller = BuildController(Guid.NewGuid());
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Comment { Id = Guid.NewGuid(), UserId = Guid.NewGuid() });

        var result = await controller.Update(Guid.NewGuid(), new UpdateCommentDto { Content = "x", Rating = 3 });

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_review_sets_UpdatedAt_and_new_rating()
    {
        var userId = Guid.NewGuid();
        var controller = BuildController(userId);
        var existing = new Comment { Id = Guid.NewGuid(), UserId = userId, ParentId = null, Rating = 2, Content = "old" };
        _repo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

        var result = await controller.Update(existing.Id, new UpdateCommentDto { Content = "new", Rating = 5 });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(5, existing.Rating);
        Assert.Equal("new", existing.Content);
        Assert.NotNull(existing.UpdatedAt);
    }

    [Fact]
    public async Task Update_reply_ignores_rating()
    {
        var userId = Guid.NewGuid();
        var controller = BuildController(userId);
        var reply = new Comment { Id = Guid.NewGuid(), UserId = userId, ParentId = Guid.NewGuid(), Rating = null, Content = "old" };
        _repo.Setup(r => r.GetByIdAsync(reply.Id)).ReturnsAsync(reply);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Comment>())).ReturnsAsync((Comment c) => c);

        var result = await controller.Update(reply.Id, new UpdateCommentDto { Content = "edited", Rating = 4 });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(reply.Rating);                   // rating stays null for replies
        Assert.Equal("edited", reply.Content);
    }
}
