using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Application.TodoItems.Commands;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Application.TodoItems.DTOs.Requests;
using TaskManager.Application.TodoItems.Queries;
using TaskManager.Domain.Enums;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class TodoItemsController(IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;

        //Endpoint to mark a task as complete. 
        /// <summary>
        /// Updates a TodoItem's status. If complete, marks it as incomplete. If incomplete, marks it as complete.
        /// </summary>
        /// <param name="todoItemId"> The Guid ID of the TodoItem in question. </param>
        /// <returns>A TodoItemEntry object that represents the TodoItem with its updated status</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     PATCH /api/todoItems/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
        /// 
        /// </remarks>
        /// 
        /// <response code="201">TodoItem status updated successfully.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>    
        /// <response code="403">The user is authenticated but does not own the project the TodoItem belongs to, nor are they the 
        /// assignee listed on the TodoItem.</response>
        /// <response code="404">The TodoItem does not exist.</response>
        [HttpPatch("{todoItemId:guid}/status")]
        public async Task<ActionResult<TodoItemEntry>> UpdateTodoItemStatus([FromRoute] Guid todoItemId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new UpdateTodoItemStatusCommand(userId, todoItemId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.TodoItemNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden or ErrorCode.ObjectDeleted => Forbid(),
                _ => result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        /// <summary>
        /// Deletes the task associated with the provided Guid ID.
        /// </summary>
        /// <param name="todoItemId"> The ID of the TodoItem, passed via the route</param>
        /// <returns>Either a success or error message in the result</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     DELETE /api/todoItems/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
        /// 
        /// </remarks>
        ///
        /// <response code="200">TodoItem deleted successfully.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the Project or TodoItem.</response>
        /// <response code="404">The TodoItem or its Project does not exist.</response>
        [HttpDelete("{todoItemId:guid}")]
        public async Task<ActionResult> DeleteTodoItemAsync([FromRoute] Guid todoItemId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new DeleteTodoItemCommand(userId, todoItemId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.TodoItemNotFound or ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.SuccessMessage)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }

        /// <summary>
        /// Retrieves the TodoItems assigned to the user.
        /// </summary>
        /// <returns>
        /// Returns a list of TodoItemEntry objects representing the TodoItems assigned to the user.
        /// </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     GET /api/todoItems/MyAssignedTasks
        /// 
        /// </remarks>
        /// <response code="200">Assigned TodoItems retrieved successfully.</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        [HttpGet("MyAssignedTasks")]
        public async Task<ActionResult<List<TodoItemEntry>>> GetAssignedTodoItems()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var query = new GetAssignedTodoItemsQuery(userId);
            var result = await _mediator.Send(query);

            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage });
        }

        /// <summary>
        /// Currently unused endpoint and route parameter, but in place as a starting point for future implementation
        /// when a TodoItem can't reasonably be pulled when going to the ProjectDetailedView page.
        /// Would allow for 
        /// </summary>
        /// <param name="todoItemId"> The Guid ID for the TodoItem</param>
        /// <param name="projectId"> The Guid ID for the Project the TodoItem belongs to - used when checking if the item is cached in Redis. </param>
        /// <returns>A DTO containing the details for the TodoItem</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     GET /api/todoItems
        ///         {
        ///             "userId":"5755cfcb-0255-4832-84d3-d754f73e902b"
        ///             "projectId":"e6137fd6-ae0b-4f6b-85c8-137fac9cef18"
        ///             "todoItemId":"19ba506e-9a32-44a4-82a3-72b36697812b"
        ///         }
        /// </remarks>
        /// <response code="200">TodoItem retrieved successfully.</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the TodoItem, nor are they the listed assignee.</response>
        /// <response code="404">The TodoItem does not exist.</response>
        [HttpGet("{projectId:guid}/{todoItemId:guid}", Name="GetTodoItem")]
        public async Task<ActionResult<TodoItemEntry>> GetTodoItemDetailedView([FromRoute] Guid todoItemId, [FromRoute] Guid projectId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);

            var query = new GetTodoItemDetailedViewQuery { TodoItemId = todoItemId, UserId = userId, ProjectId = projectId};

            var result = await _mediator.Send(query);

            return result.ErrorCode switch
            {
                ErrorCode.TodoItemNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }
        
        /// <summary>
        /// Updates a TodoItem's details.
        /// </summary>
        /// <param name="todoItemId"> The Guid ID of the TodoItem, passed via the route</param>
        /// <param name="request"> An UpdateTodoItemRequest object that contains the details of the updated TodoItem </param>
        /// <returns> A TodoItemEntry object representing the updated TodoItem</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     PATCH /api/todoItems/0189a61a-f313-4b88-a765-7acc5d33bf75
        ///         {
        ///             "projectId":"e6137fd6-ae0b-4f6b-85c8-137fac9cef18"
        ///             "title":"Updated Title"
        ///             "description":"Updated Description"
        ///             "assigneeId":"095531fb-eae1-4a48-ac56-a63b18d8d2f2"
        ///             "status":"0"
        ///             "priority":"1"
        ///             "dueDate":"2026-12-31T23:59:59Z"
        ///         }
        /// </remarks>
        /// <response code="200">TodoItem retrieved successfully.</response>
        /// <response code="400">If the query fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the TodoItem, nor are they the listed assignee.</response>
        /// <response code="404">The TodoItem does not exist.</response>
        [HttpPatch("{todoItemId:guid}")]
        public async Task<ActionResult<TodoItemEntry>> UpdateTodoItem([FromRoute] Guid todoItemId, [FromBody] UpdateTodoItemRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new UpdateTodoItemCommand(userId, request.ProjectId, todoItemId, request.AssigneeId, request.Title, request.Description, 
                request.Priority, request.DueDate);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.TodoItemNotFound or ErrorCode.ProjectNotFound or ErrorCode.AssigneeNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                ErrorCode.TitleError or ErrorCode.DescriptionError => BadRequest(new { Message = result.ErrorMessage }),
                
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode, Message = result.ErrorMessage })
            };
        }
    }
}
