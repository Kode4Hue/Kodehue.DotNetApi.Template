using Mediator;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Common.Response;
using System.Net;

namespace WebApi.Common.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly IMediator Mediator;

        public BaseApiController(IMediator mediator)
        {
            Mediator = mediator;
        }

        /// <summary>
        /// Returns a standardized success response (200 OK).
        /// </summary>
        protected IActionResult Success(object? data = null, string? message = null)
        {
            return Ok(new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Success = true,
                Message = message ?? "Request completed successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Returns a standardized created response (201 Created).
        /// </summary>
        protected IActionResult Created(string routeName, object? routeValues, object? data = null)
        {
            return CreatedAtRoute(routeName, routeValues, new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.Created,
                Success = true,
                Message = "Resource created successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Returns a standardized not found response (404).
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "Resource not found.")
        {
            return NotFound(new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Success = false,
                Message = message
            });
        }

        /// <summary>
        /// Returns a standardized bad request response (400).
        /// </summary>
        protected IActionResult BadRequestResponse(string message = "Invalid request.", object? details = null)
        {
            return BadRequest(new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Success = false,
                Message = message,
                Data = details
            });
        }

        /// <summary>
        /// Returns a standardized internal server error response (500).
        /// </summary>
        protected IActionResult ErrorResponse(string message = "An unexpected error occurred.", object? details = null)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Success = false,
                Message = message,
                Data = details
            });
        }
    }
}
