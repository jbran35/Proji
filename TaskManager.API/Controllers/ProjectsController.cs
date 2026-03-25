using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Application.Projects.Commands;
using TaskManager.Application.Projects.DTOs;
using TaskManager.Application.Projects.DTOs.Requests;
using TaskManager.Application.Projects.DTOs.Responses;
using TaskManager.Application.Projects.Queries;
using TaskManager.Application.TodoItems.DTOs;
using TaskManager.Domain.Enums;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController (IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;


        /// <summary>
        /// Marks a specific project and all of its (incomplete) todo items as complete. 
        /// </summary>
        /// <param name="projectId"> The Guid ID for the project, passed in the route </param>
        /// <returns> If successful, a response containing the updated ProjectTileDto for the completed project. 
        /// Otherwise, returns an error message 
        /// </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim. 
        /// 
        /// Sample request:
        ///     PATCH /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6/status
        /// </remarks>
        /// 
        /// <response code="200">Successfully completed the project.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>

        [HttpPatch("{projectId:guid}/status")]
        public async Task<ActionResult<CompleteProjectResponse>> Complete([FromRoute] Guid projectId)
        {
            //Validate User Identity
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new CompleteProjectCommand(userId, projectId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }

        /// <summary>
        /// Creates a new project for the user using the passed details. 
        /// </summary>
        /// <param name="request"> A CreateProjectRequest object that is passed in the request body</param>
        /// <returns> If successful, a ProjectTileDto representing the newly created project. Otherwise, an error message</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.
        /// 
        /// Constraints:
        /// - Title: Required, must be between 1 and 100 characters.
        /// - Description: Optional, maximum 500 characters.
        /// 
        /// Sample Request:
        /// 
        ///     POST /api/projects/
        ///     {
        ///        "title": "My New Project",
        ///        "description": "This is my new project..."
        ///     }      
        /// </remarks>
        /// <response code="201">Returns the details for the newly created project.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>        
        [HttpPost]
        public async Task<ActionResult<ProjectTileDto>> Create([FromBody] CreateProjectRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "Unauthorized" });

            var userId = Guid.Parse(userIdString);
            var command = new CreateProjectCommand(userId, request.Title, request.Description);
            var result = await _mediator.Send(command);

            if (result.ErrorCode == ErrorCode.UserNotFound)
                return Unauthorized(new { Message = result.ErrorMessage });

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    Code = result.ErrorCode.ToString(),
                    Message = result.ErrorMessage
                });
            }

            return CreatedAtAction(
                nameof(GetProjectDetailedView), 
                new { projectId = result.Value.Id }, 
                result.Value);        }


        /// <summary>
        /// Deletes a specific project owned by the user. 
        /// </summary>
        /// <param name="projectId">The Guid ID for the project in question - passed via the route </param>
        /// <returns> If successful, returns a DeleteProjectResponse object (a response DTO containing the Guid of a
        /// successfully deleted project and message). Otherwise, returns an error message.
        ///</returns>
        ///<remarks> 
        ///
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.
        /// 
        ///  Sample Request: 
        ///  
        ///     DELETE /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// 
        /// </remarks>
        /// <response code="200">The project was deleted successfully.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the
        /// database or validation of the query.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>
        [HttpDelete("{projectId:guid}")]
        public async Task<ActionResult<DeleteProjectResponse>> Delete([FromRoute] Guid projectId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new DeleteProjectCommand(userId, projectId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }


        /// <summary>
        /// Retrieves a project (its details, todo items, and todo item counts). 
        /// </summary>
        /// <param name="projectId"> The Guid ID for the project, passed in the route </param>
        /// <returns> If successful, returns a GetProjectDetailedViewResponse, which holds the ProjectDetailedViewDto
        /// corresponding to the requested project. Otherwise, returns an error message </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.        
        /// 
        ///Sample Request: 
        ///  
        ///     GET /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// 
        /// </remarks>
        /// <response code="200">Successfully retrieved the project and its details/todo items.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the
        /// database or validation of the query.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>
        /// 
        [HttpGet("{projectId:guid}")]
        public async Task<ActionResult<GetProjectDetailedViewResponse>> GetProjectDetailedView([FromRoute] Guid projectId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new GetProjectDetailedViewQuery(userId, projectId);
            var result = await _mediator.Send(command);
            
            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }


        /// <summary>
        /// Retrieves the basic details for a project (i.e., ID, Title, Description, CreatedOn DateTime)
        /// </summary>
        /// <param name="projectId"> The Guid ID for the requested project </param>
        /// <returns> A ProjectDetailsDto, containing the four properties in question </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.   
        /// 
        /// Sample Request: 
        ///  
        ///     GET /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6/details
        /// 
        /// </remarks>
        /// <response code="200">Successfully retrieved the project's basic details.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response> 
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>
        [HttpGet("{projectId:guid}/details")]
        public async Task<ActionResult<ProjectDetailsDto>> GetProjectDetailsAsync([FromRoute] Guid projectId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new GetProjectDetailsQuery(userId, projectId);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), Message = result.ErrorMessage })
            };
        }


        /// <summary>
        /// Retrieves a list of a user's projects in ProjectTileDto format. 
        /// </summary>
        /// <returns>A list of ProjectTileDto objects to be displayed on the user's dashboard/home page</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.   
        /// 
        /// Sample Request: 
        ///  
        ///     GET /api/projects/MyProjects
        /// 
        /// </remarks>
        /// <response code="200">Successfully retrieved the user's project tiles.</response>
        /// <response code="400">Issue with validation of the request.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response> 
        [HttpGet("MyProjects")]
        public async Task<ActionResult<List<ProjectTileDto>>> GetUserProjects()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new GetUserProjectsQuery(userId);
            var result = await _mediator.Send(command);

            return result.IsSuccess ? Ok(result.Value) : BadRequest(new
            {
                Code = result.ErrorCode.ToString(),
                result.ErrorMessage
            });
        }

        /// <summary>
        /// Retrieves a project (its details, todo items, and todo item counts). 
        /// </summary>
        /// <param name="projectId"> The Guid ID for the project, passed in the route </param>
        /// <returns> If successful, returns a GetProjectDetailedViewResponse, which holds the ProjectDetailedViewDto
        /// corresponding to the requested project. Otherwise, returns an error message </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.        
        /// 
        ///Sample Request: 
        ///  
        ///     GET /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// 
        /// </remarks>
        /// <response code="200">Successfully retrieved the project and its details and todo items.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the
        /// database or validation of the query.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>
        [HttpGet("{projectId:guid}/tasks")]
        public async Task<ActionResult<ProjectDetailedViewDto>> GetProjectTodoItems([FromRoute] Guid projectId)
        {
            //Validate user identity
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var query = new GetProjectDetailedViewQuery(userId, projectId);
            var result = await _mediator.Send(query);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }

        /// <summary>
        /// Updates a project's details (title, description). 
        /// </summary>
        /// <param name="projectId"> The Guid ID of the project, passed via the route</param>
        /// <param name="request"> An UpdateProjectRequest object, passed via the request body - contains the
        /// Title/Description for the project</param>
        /// <returns>If successful, returns a ProjectDetailsDto containing the properties for the project.
        /// Otherwise, returns an error message.</returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.        
        /// 
        /// Sample Request: 
        ///  
        ///     PATCH /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6
        ///
        ///     PATCH /api/projects/
        ///         {
        ///             "title": "Project Title",
        ///             "description": "Project Description"
        ///         }      
        /// 
        /// </remarks>
        /// <response code="200">The project was updated successfully.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the
        /// database or validation of the query.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>
        /// <response code="403">The user is authenticated but does not own the project.</response>
        /// <response code="404">The project does not exist.</response>
        [HttpPatch("{projectId:guid}")]
        public async Task<ActionResult<ProjectDetailsDto>> Update([FromRoute] Guid projectId, [FromBody] UpdateProjectRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new UpdateProjectCommand(userId, projectId, request.Title, request.Description);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                ErrorCode.TitleError or ErrorCode.DescriptionError => BadRequest(new {Message = result.ErrorMessage}),
                _ => result.IsSuccess
                    ? Ok(result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }


        /// <summary>
        /// Creates a new Todo Item and adds it to its parent project. 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="request"> A CreateTodoItemRequest object that is passed in the request body</param>
        /// 
        /// <returns> If successful, a TodoItemEntry object representing the newly created TodoItem, so it can
        /// be displayed in the project's TaskLIst. Otherwise, an error message </returns>
        /// <remarks>
        /// 
        /// This endpoint requires an authenticated user session via a JWT.The Project's owner ID is 
        /// automatically extracted from the token's 'NameIdentifier' claim.
        /// 
        /// Constraints:
        /// - Title: Required, must be between 1 and 100 characters.
        /// - Description: Optional, maximum 500 characters.
        /// 
        /// Sample Request:
        /// 
        ///     POST /api/projects/{projectId}/tasks
        ///     {
        ///        "title": "TodoItem Title",
        ///        "projectId": "71962e2a-15db-4601-9d99-35de2837798a",
        ///        "description": "This is my new TodoItem...",
        ///        "assigneeId": "a5f58cae-16ff-46e3-94f8-0d3f33e29f26",
        ///        "status": "0",
        ///        "priority": "1",
        ///        "dueDate": "2026-12-31T23:59:59Z"
        ///     }      
        /// </remarks>
        /// <response code="201">Returns the details for the newly created TodoItem.</response>
        /// <response code="400">If the command fails due to a domain logic violation or if there is an issue with the database.</response>
        /// <response code="401">If the user is not authenticated or the User ID is missing from the JWT token.</response>    
        /// <response code="403">The user is authenticated but does not own the project the TodoItem is being added to. </response>
        /// <response code="404">The project in question does not exist.</response>
        [HttpPost("{projectId:guid}/tasks")]
        public async Task<ActionResult<TodoItemEntry>> AddTodoItem([FromRoute] Guid projectId, [FromBody] CreateTodoItemRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { Message = "User ID not found in token" });

            var userId = Guid.Parse(userIdString);
            var command = new AddTodoItemCommand(projectId, userId, request.AssigneeId, request.Title, request.Description, request.DueDate, request.Priority);
            var result = await _mediator.Send(command);

            return result.ErrorCode switch
            {
                ErrorCode.UserNotFound => Unauthorized(new { Message = result.ErrorMessage }),
                ErrorCode.ProjectNotFound or ErrorCode.AssigneeNotFound => NotFound(new { Message = result.ErrorMessage }),
                ErrorCode.Forbidden => Forbid(),
                ErrorCode.TitleError or ErrorCode.DescriptionError or ErrorCode.DomainRuleViolation => BadRequest(new {Message = result.ErrorMessage}),
                _ => result.IsSuccess
                    ? CreatedAtAction("GetTodoItemDetailedView", "TodoItems", new { projectId, todoItemId = result.Value.Id }, result.Value)
                    : BadRequest(new { Code = result.ErrorCode.ToString(), result.ErrorMessage })
            };
        }
    }
}
