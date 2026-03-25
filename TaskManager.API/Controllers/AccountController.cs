using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.API.DTOs.Account;
using TaskManager.Application.UserConnections.Commands;
using TaskManager.Application.UserConnections.DTOs;
using TaskManager.Application.UserConnections.DTOs.Requests;
using TaskManager.Application.UserConnections.Queries;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.DTOs.Requests;
using TaskManager.Application.Users.DTOs.Responses;
using TaskManager.Application.Users.Queries;
using TaskManager.Domain.Enums;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator; 

        //Delete endpoint for removing an assignee connection.
        /// <summary>
        /// Deletes an assignee from a user's group.
        /// </summary>
        /// <param name="connectionId">The Guid ID for the UserConnection representing the relationship between the user and the assignee</param>
        /// <returns>If successful, returns a success code, otherwise returns an error message</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     DELETE /api/account/0189a61a-f313-4b88-a765-7acc5d33bf75
        ///     
        /// </remarks>
        /// <response code="200">UserConnection deleted successfully (i.e., Assignee removed from the User's group successfully).</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the TodoItem, nor are they the listed assignee.</response>
        /// <response code="404">The UserConnection does not exist.</response>
        [Authorize]
        [HttpDelete("assignees/{connectionId:guid}")]
        public async Task<ActionResult> DeleteAssigneeAsync([FromRoute] Guid connectionId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Unauthorized" });

            var userId = Guid.Parse(userIdString);
            var command = new DeleteUserConnectionCommand(userId, connectionId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ConnectionNotFound or ErrorCode.AssigneeNotFound => NotFound(new { Message = result.ErrorCode }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        /// <summary>
        /// Creates a new UserConnection (i.e., adds a new assignee to the User's group).
        /// </summary>
        /// <param name="connectionRequest"> </param>
        /// <returns>A request DTO containing the Guid ID for the assignee the User is trying to add to their group. </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     POST /api/account/assignees
        ///
        ///     {
        ///         "assigneeId":"aae4ea49-b6c4-4060-8570-cfb37dd28072"
        ///     }
        ///     
        /// </remarks>
        /// <response code="200">UserConnection created successfully (i.e., the Assignee has been added to the User's group successfully).</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="404">The assignee does not exist.</response>
        [Authorize]
        [HttpPost("assignees")]
        public async Task<ActionResult<UserConnectionDto>> AddAssignee([FromBody] CreateUserConnectionRequest connectionRequest)
        {
            //Validate user is authenticated and get user id from claims
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Unauthorized" });

            var userId = Guid.Parse(userIdString);
            var command = new CreateUserConnectionCommand(userId, connectionRequest.AssigneeId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.AssigneeNotFound => NotFound(new { Message = result.ErrorCode }),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }
        
        
        /// <summary>
        /// Retrieves all the assignees in the User's group (i.e., all of their UserConnections)
        /// </summary>
        /// <returns> A list of UserConnectionDto objects - representing the assignees in the User's group</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///    
        ///     GET /api/account/assignees
        ///
        /// </remarks>
        /// <response code="200">User's connections/assignees were retrieved successfully.</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        [Authorize]
        [HttpGet("assignees")]
        public async Task<ActionResult<List<UserConnectionDto>>> GetAssignees()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Unauthorized" });

            var userId = Guid.Parse(userIdString);
            var query = new GetActiveUserConnectionsQuery(userId);
            var result = await _mediator.Send(query);

            return result.ErrorCode switch
            {
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }


        //Endpoint for finding a user by email to add as an assignee connection.
        /// <summary>
        /// Searches for a user by their email to verify that they exist. Used when a user goes to add another user to their group (as an assignee).
        /// </summary>
        /// <param name="userEmail"> The email for the assignee-to-be, passed via the route</param>
        /// <returns> A GetUserResponse object that returns the Guid ID, FirstName, LastName, and Email of the assignee-to-be</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     GET /api/account/user@email.com
        /// 
        /// </remarks>
        /// <response code="200">User found, and their details returned, successfully </response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="404">If the user is not found.</response>
        [Authorize]
        [HttpGet("{userEmail}")]
        public async Task<ActionResult<GetUserResponse>> FindUser([FromRoute] string userEmail)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var query = new GetUserQuery(userEmail);
            var result = await _mediator.Send(query);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => NotFound(new { Message = result.ErrorMessage }),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        /// <summary>
        /// Authenticates the user's credentials.
        /// </summary>
        /// <param name="command">LoginUserCommand record containing the entered UserName/Password</param>
        /// <returns>A JWT in the LoginUserResponse object, if successful</returns>
        /// <remarks>
        /// 
        ///     POST /api/account/login
        ///         {
        ///             "username":"james"
        ///             "password":"Password123!"
        ///         }
        /// </remarks>
        /// <response code="200">User was authenticated successfully </response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If authentication failed.</response>
        /// 
        [HttpPost("login")]
        public async Task<ActionResult<LoginUserResponse>> Login([FromBody] LoginUserCommand command)
        {
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.AuthError => Unauthorized(new { Message = result.ErrorMessage }),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        /// <summary>
        /// Registers a new user using the details provided.
        /// </summary>
        /// <param name="model">Model object containing the details needed to create a new user account</param>
        /// <returns>A success or error message</returns>
        /// <remarks>
        /// 
        ///  POST /api/account/register
        ///         {
        ///             "username":"johnDoe"
        ///             "email":"johndoe@email.com"
        ///             "password":"Password123!"
        ///             "firstName":"John"
        ///             "lastName":"Doe"
        ///         }
        /// 
        /// </remarks>
        /// <response code="200">User was authenticated successfully </response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        [HttpPost("registration")]
        public async Task<ActionResult> Register([FromBody] RegisterModel? model)
        {
            if (!ModelState.IsValid || model is null)
                return BadRequest(ModelState);

            var command = new RegisterUserCommand(model.Username, model.Password, model.Email, model.FirstName, model.LastName);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        
        /// <summary>
        /// Updates a user's profile details (FirstName, LastName, Email, UserName).
        /// </summary>
        /// <param name="request">An UpdateProfileRequest object containing the details for the user's updated profile,
        /// including their: Guid ID, FirstName, LastName, UserName, Email </param>
        /// <returns>An UpdateProfileResponse object containing the UserProfileDto for the updated profile,
        /// as well as the user's updated token. </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     PATCH /api/account/profile
        ///
        ///     {
        ///         "id":"0b660a38-90da-474d-8953-701827bb5c90"
        ///         "firstName":"Joe"
        ///         "lastName":"Smith"
        ///         "email":"joesmith@email.com"
        ///         "userName":"joeSmith"
        ///     }
        /// </remarks>
        /// <response code="200">User's profile details were updated successfully.</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="404">If the user is not found.</response>
        [Authorize]
        [HttpPatch("profile")]
        public async Task<ActionResult<UpdateProfileResponse>> UpdateUserInfo([FromBody] UpdateProfileRequest request)
        {   
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Unauthorized" });

            var userId = Guid.Parse(userIdString);
            if (userId != Guid.Empty)
                request.Id = userId;

            if (request.Id == Guid.Empty || request.Id != userId)
                return BadRequest(new { Message = "Bad Request" });

            var command = new UpdateProfileCommand(
                request.Id, request.FirstName, request.LastName, request.Email, request.UserName);

            var result = await _mediator.Send(command);
            
            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => NotFound(new { Message = result.ErrorMessage }),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }
    }
}