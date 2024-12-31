using AutoMapper;
using BusinessLayer.Requests;
using BusinessLayer.Responses;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using LMSApi.App.Atrributes;
using LMSApi.App.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace LMSApi.App.Controllers.Auth
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtOptions _jwtOptions;
        private readonly IMapper mapper;

        public AuthController(AppDbContext appDbContext, ILogger<AuthController> logger, JwtOptions jwtOptions, IMapper mapper)
        {
            _appDbContext = appDbContext;
            _logger = logger;
            _jwtOptions = jwtOptions;
            this.mapper = mapper;
        }

        [HttpPost]
        [Route("login")]
        [CheckPermission("Class.index")]
        public async Task<ActionResult<ApiResponse<string>>> Login(LoginRequest request)
        {
            var user = await _appDbContext.Users.Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return NotFound(ApiResponseFactory.Create("User not found", 404, false));

            if (!VerifyPassword(user.Password, request.Password))
                return BadRequest(ApiResponseFactory.Create("Invalid password", 400, false));

            return Ok(ApiResponseFactory.Create((object)GenerateJwtToken(user, _jwtOptions), "Succeeded", 201, true));
        }

        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<ApiResponse<string>>> Register(UserRequest request)
        {
            // Check if a user with the same email or phone exists
            var existingUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.Phone == request.Phone);
            if (existingUser != null)
                return BadRequest(ApiResponseFactory.Create("User already exists", 400, false));

            // Get or create the default "User" role
            var userRole = await _appDbContext.Roles.Include(u => u.Permissions).SingleOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                userRole = new Role { Name = "User" };
                await _appDbContext.Roles.AddAsync(userRole);
                await _appDbContext.SaveChangesAsync();  // Save changes to get the new role's ID
            }

            // Hash the user's password
            var passwordHasher = new PasswordHasher<User>();
            var hashedPassword = passwordHasher.HashPassword(null, request.Password);  // Hash the provided password

            // Map the request to the User entity
            User user = mapper.Map<User>(request);
            user.Password = hashedPassword;  // Set the hashed password
            user.Roles = new List<Role> { userRole };

            // Add the user to the database
            await _appDbContext.Users.AddAsync(user);
            await _appDbContext.SaveChangesAsync();

            // Prepare the data to return
            Dictionary<string, object> data = new Dictionary<string, object>
    {
        { "Token", GenerateJwtToken(user, _jwtOptions) },
        { "Permission", userRole.Permissions.Select(p => p.RouteName).ToList() }
    };

            return Ok(ApiResponseFactory.Create(
                data.Select(kvp => new { key = kvp.Key, value = kvp.Value }).ToList(),
                "User registered successfully",
                201,
                true));
        }

        [HttpGet]
        [Route("{userId}")]
        [Authorize]
        [CheckPermission("users.show")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int userId)
        {
            var user = await _appDbContext.Users
                .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(ApiResponseFactory.Create("User not found", 404, false));

            var userDto = mapper.Map<UserDto>(user);

            return Ok(userDto);
        }

        // In your AuthController

        [ApiExplorerSettings(IgnoreApi = true)]
        public string GenerateJwtToken(User user, JwtOptions jwtOptions)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Name, user.FirstName),
    };

            var userPermissions = new HashSet<string>();

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));

                // Gather permissions for the user's roles
                foreach (var permission in role.Permissions)
                {
                    userPermissions.Add(permission.RouteName);
                }
            }

            // Add permissions as a single custom claim
            claims.Add(new Claim("permissions", string.Join(",", userPermissions)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtOptions.DurationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        [ApiExplorerSettings(IgnoreApi = true)]
        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var passwordHasher = new PasswordHasher<User>();  // Use the correct type here
            var result = passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }


}
