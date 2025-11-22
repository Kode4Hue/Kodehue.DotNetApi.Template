using FiWi.Users.Service.Account.Controllers;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Application.Account.Users.Requests.GetUserById;
using Application.Account.Users.DTOs;
using Application.Common.Results;
using System.Threading.Tasks;

namespace FiWi.Users.Service.Tests
{
    public class UserControllerTests
    {
        [Fact]
        public async Task GetUserById_ReturnsNotFound_WhenUserNull()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>())).ReturnsAsync((Application.Account.Users.DTOs.UserProfile?)null);

            var controller = new UserController(mediatorMock.Object);

            var result = await controller.GetUserById("nonexistent");

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task GetUserById_ReturnsOk_WhenUserFound()
        {
            var mediatorMock = new Mock<IMediator>();
            var profile = new Application.Account.Users.DTOs.UserProfile { Id = "1", Email = "test@example.com", IdentityUserId = "iid" };
            mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>())).ReturnsAsync(profile);

            var controller = new UserController(mediatorMock.Object);

            var result = await controller.GetUserById("1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }
    }
}
