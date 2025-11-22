using Application.Account.Users.Requests.GetUserById;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using WebApi.Common.Controllers;
using WebApi.Common.Controllers.Constants;

namespace WebApi.Account.Controllers
{
    public class UserController : BaseApiController
    {
        public UserController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Retrieves a user by ID.
        /// </summary>
        [HttpGet("{id}", Name = ControllerRouteNames.GetUserById)]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await Mediator.Send(new GetUserByIdQuery(id));

            if (user == null)
                return NotFoundResponse($"User with ID {id} not found.");

            return Success(user, "User retrieved successfully.");
        }
    }
}
