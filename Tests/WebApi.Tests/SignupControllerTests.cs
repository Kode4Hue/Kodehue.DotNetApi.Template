using Application.Account.Users.Requests.SignupAttendee;
using Application.Account.Users.Requests.SignupAttendee.DTOs;
using Application.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SharedLibrary.Account.Signup.Attendee;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Account.Controllers;
using Xunit;

namespace WebApi.Tests
{
    public class SignupControllerTests
    {
        [Fact]
        public async Task RegisterUser_ReturnsCreated_WhenSuccess()
        {
            var mediatorMock = new Mock<IMediator>();
            var response = new SignupAttendeeResponse("123");
            var result = Result<SignupAttendeeResponse>.CreateSuccessResult(response);
            mediatorMock.Setup(m => m.Send(It.IsAny<SignupAttendeeCommand>())).ReturnsAsync(result);

            var controller = new SignupController(mediatorMock.Object);

            var request = new SignupAttendeeRequest { Email = "test@example.com", Password = "secret" };

            var actionResult = await controller.RegisterUser(request);

            var createdResult = Assert.IsType<CreatedAtRouteResult>(actionResult);
            Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        }

        [Fact]
        public async Task RegisterUser_ReturnsBadRequest_WhenValidationError()
        {
            var mediatorMock = new Mock<IMediator>();
            var errors = new List<Application.Common.Results.ErrorDto> { Application.Common.Results.ErrorDto.CreateFromMessage("Invalid") };
            var result = Result<SignupAttendeeResponse>.CreateErrorResult(errors, Application.Common.Results.ErrorType.ValidationError);
            mediatorMock.Setup(m => m.Send(It.IsAny<SignupAttendeeCommand>())).ReturnsAsync(result);

            var controller = new SignupController(mediatorMock.Object);
            var request = new SignupAttendeeRequest { Email = "", Password = "" };

            var actionResult = await controller.RegisterUser(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        }

        [Fact]
        public async Task RegisterUser_ReturnsConflict_WhenConflictError()
        {
            var mediatorMock = new Mock<IMediator>();
            var errors = new List<Application.Common.Results.ErrorDto> { Application.Common.Results.ErrorDto.CreateFromMessage("Conflict") };
            var result = Result<SignupAttendeeResponse>.CreateErrorResult(errors, Application.Common.Results.ErrorType.Conflict);
            mediatorMock.Setup(m => m.Send(It.IsAny<SignupAttendeeCommand>())).ReturnsAsync(result);

            var controller = new SignupController(mediatorMock.Object);
            var request = new SignupAttendeeRequest { Email = "test@example.com", Password = "secret" };

            var actionResult = await controller.RegisterUser(request);

            var status = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status409Conflict, status.StatusCode);
        }

        [Fact]
        public async Task RegisterUser_ReturnsServerError_WhenApplicationError()
        {
            var mediatorMock = new Mock<IMediator>();
            var errors = new List<Application.Common.Results.ErrorDto> { Application.Common.Results.ErrorDto.CreateFromMessage("AppErr") };
            var result = Result<SignupAttendeeResponse>.CreateErrorResult(errors, Application.Common.Results.ErrorType.ApplicationError);
            mediatorMock.Setup(m => m.Send(It.IsAny<SignupAttendeeCommand>())).ReturnsAsync(result);

            var controller = new SignupController(mediatorMock.Object);
            var request = new SignupAttendeeRequest { Email = "test@example.com", Password = "secret" };

            var actionResult = await controller.RegisterUser(request);

            var status = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
        }
    }
}
