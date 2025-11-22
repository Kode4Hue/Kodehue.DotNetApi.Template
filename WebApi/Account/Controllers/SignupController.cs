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
                    return BadRequest(result.Errors);
                else if (result.ErrorType!.Equals(ErrorType.Conflict))
                {
                    return StatusCode(
                        StatusCodes.Status409Conflict,
                        result.Errors);
                }
                else
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        result.Errors);
                }
            }

            return CreatedAtRoute(
                routeName: ControllerRouteNames.GetUserById,
                routeValues: new { id = result.Content.UserProfileId },
                value: new { result.Content.UserProfileId }
            );
        }
    }
}

