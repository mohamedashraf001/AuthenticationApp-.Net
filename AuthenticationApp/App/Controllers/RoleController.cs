using BusinessLayer.Interfaces;
using BusinessLayer.Requests;
using BusinessLayer.Responses;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using LMSApi.App.Atrributes;
using LMSApi.App.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SharedClasses.Exceptions;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;
    private readonly AppDbContext _dbContext;

    public RolesController(IRoleService roleService, AppDbContext dbContext, ILogger<RolesController> logger)
    {
        _dbContext = dbContext;
        _roleService = roleService;
        _logger = logger;
    }

    private RoleResponse MapRoleToRoleResponse(Role role)
    {
        return new RoleResponse
        {
            Name = role.Name,
            Permissions = role.Permissions.Select(p => new PermissionResponse
            {
              
                Name = p.Name,
                Category = p.Category,
                RouteName=p.RouteName
            }).ToList()
        };
    }

    [HttpGet]
    [Route("")]
  //  [CheckPermission("Class.index")]
    public async Task<ActionResult<IApiResponse>> GetAllRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            var roleResponses = roles.Select(MapRoleToRoleResponse).ToList();
            return Ok(ApiResponseFactory.Create(roleResponses, "Roles fetched successfully", 200, true));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while fetching roles.");
            return StatusCode(500, ApiResponseFactory.Create(ex.Message, 500, false));
        }
    }

    [HttpGet]
    [Route("{roleId}")]
    public async Task<ActionResult<IApiResponse>> GetRoleById(int roleId)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            var roleResponse = MapRoleToRoleResponse(role);
            return Ok(ApiResponseFactory.Create(roleResponse, "Role fetched successfully", 200, true));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponseFactory.Create(ex.Message, 404, false));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while fetching role with id {roleId}.", roleId);
            return StatusCode(500, ApiResponseFactory.Create(ex.Message, 500, false));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponseSingleStrategy>> CreateRole([FromBody] CreateRoleRequest roleRequest)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(roleRequest);

            // Manually map Role to RoleResponse
            var roleResponse = new RoleResponse
            {
                Name = role.Name,
                Permissions = role.RolePermissions
                                  .Select(rp => new PermissionResponse
                                  {
                                      
                                      Name = rp.Permission.Name,
                                      Category = rp.Permission.Category,
                                      RouteName =rp.Permission.RouteName,
                                  }).ToList()
            };

            return Ok(ApiResponseFactory.Create(roleResponse, "Role created successfully", 201, true));
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ApiResponseFactory.Create(ex.Message, 400, false));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while creating role. Log message: {logMessage}", ex);
            return StatusCode(500, ApiResponseFactory.Create(ex.Message, 500, false));
        }
    }


    [HttpPut]
    [Route("{roleId}")]
    public async Task<ActionResult<IApiResponse>> UpdateRole(int roleId, [FromBody] CreateRoleRequest roleRequest)
    {
        try
        {
            var updatedRole = await _roleService.UpdateRoleAsync(roleId, roleRequest);
            var roleResponse = MapRoleToRoleResponse(updatedRole);
            return Ok(ApiResponseFactory.Create(roleResponse, "Role updated successfully", 200, true));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponseFactory.Create(ex.Message, 404, false));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while updating role with id {roleId}.", roleId);
            return StatusCode(500, ApiResponseFactory.Create(ex.Message, 500, false));
        }
    }
    [HttpPost]
    [Route("assign")]
    public async Task<ActionResult<IApiResponse>> AssignRoleToUser([FromBody] AssignRoleRequest request)
    {
        try
        {
            _logger.LogInformation("Received request to assign role with ID {RoleId} to user with ID {UserId}.", request.RoleId, request.UserId);

            // Check if the role exists
            var roleExists = await _roleService.GetRoleByIdAsync(request.RoleId) != null;
            if (!roleExists)
            {
                _logger.LogWarning("Role with ID {RoleId} does not exist.", request.RoleId);
                return NotFound(ApiResponseFactory.Create("Role not found", 404, false));
            }

            // Check if the user exists (You might need to implement a method for this in your service/repository)
            var userExists = await _dbContext.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                _logger.LogWarning("User with ID {UserId} does not exist.", request.UserId);
                return NotFound(ApiResponseFactory.Create("User not found", 404, false));
            }



            // Check if the role is already assigned to the user
            var isAssigned = await _roleService.IsRoleAssignedToUserAsync(request.UserId, request.RoleId);
            if (isAssigned)
            {
                _logger.LogWarning("Role with ID {RoleId} is already assigned to user with ID {UserId}.", request.RoleId, request.UserId);
                return BadRequest(ApiResponseFactory.Create("Role is already assigned to the user", 400, false));
            }

            // Assign the role to the user
            await _roleService.AddRoleToUserAsync(request.UserId, request.RoleId);

            _logger.LogInformation("Successfully assigned role with ID {RoleId} to user with ID {UserId}.", request.RoleId, request.UserId);
            return Ok(ApiResponseFactory.Create("Role assigned to user successfully", 200, true));
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while assigning role to user.");
            return StatusCode(500, ApiResponseFactory.Create("Internal server error", 500, false));
        }
    }



}
