using Application.Account.Users.Requests.SignupAttendee;
using Application.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Account.Signup.Attendee;
using WebApi.Common.Controllers;
using WebApi.Common.Controllers.Constants;

namespace WebApi.Account.Controllers
{
    public class SignupController : BaseApiController
    {
        public SignupController(IMediator mediator) : base(mediator)
        {
        }


        [HttpPost(ControllerRouteNames.SignupAttendee)]
        public async Task<IActionResult> RegisterUser([FromBody] SignupAttendeeRequest request)
        {
            var result = await Mediator.Send(new SignupAttendeeCommand
            {
                Email = request.Email ?? string.Empty,
                Password = request.Password ?? string.Empty,
            });

            if (!result.IsSuccess)
            {
                if (result.ErrorType!.Equals(ErrorType.ValidationError))
                    return BadRequestResponse("Validation failed.", result.Errors);
                else if (result.ErrorType!.Equals(ErrorType.Conflict))
                {
                    return StatusCode(
                        StatusCodes.Status409Conflict,
                        new SharedLibrary.Common.Response.ApiResponse
                        {
                            StatusCode = StatusCodes.Status409Conflict,
                            Success = false,
                            Message = "Conflict",
                            Data = result.Errors
                        });
                }
                else
                {
                    return ErrorResponse("An internal error occurred.", result.Errors);
                }
            }

            return Created(
                routeName: ControllerRouteNames.GetUserById,
                routeValues: new { id = result.Content!.UserProfileId },
                data: new { result.Content.UserProfileId }
            );
        }
    }
}

