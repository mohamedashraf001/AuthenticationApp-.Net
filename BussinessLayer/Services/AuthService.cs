//// BussinessLayer.Services.AuthService.cs
//using BussinessLayer.Dtos;
//using DataAccessLayer.Data;
//using DataAccessLayer.Entities;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//namespace BussinessLayer.Services
//{
//    public class AuthService
//    {
//        private readonly AppDbContext _dbContext;
//        private readonly IConfiguration _config;

//        public AuthService(AppDbContext dbContext, IConfiguration config)
//        {
//            _dbContext = dbContext;
//            _config = config;
//        }

//        public async Task Register(RegisterDto dto)
//        {
//            var user = new User
//            {
//                Username = dto.Username,
//                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
//                Roles = new List<Role>() // Assign roles later
//            };

//            _dbContext.Users.Add(user);
//            await _dbContext.SaveChangesAsync();
//        }

//        public async Task<string> Login(LoginDto dto)
//        {
//            var user = await _dbContext.Users
//                .Include(u => u.Roles)
//                .ThenInclude(r => r.RolePermissions)
//                .ThenInclude(rp => rp.Permission)
//                .FirstOrDefaultAsync(u => u.Username == dto.Username);

//            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
//                throw new UnauthorizedAccessException("Invalid credentials.");

//            return await GenerateToken(user);
//        }

//        private async Task<string> GenerateToken(User user)
//        {
//            var claims = new List<Claim>
//            {
//                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
//                new Claim(JwtRegisteredClaimNames.Name, user.Username)
//            };

//            var userPermissions = new HashSet<string>();

//            foreach (var role in user.Roles)
//            {
//                claims.Add(new Claim(ClaimTypes.Role, role.Name));

//                // Gather permissions for the user's roles
//                foreach (var permission in role.RolePermissions.Select(rp => rp.Permission))
//                {
//                    userPermissions.Add(permission.RouteName);
//                }
//            }

//            // Add permissions as a single custom claim
//            claims.Add(new Claim("permissions", string.Join(",", userPermissions)));

//            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
//            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//            var token = new JwtSecurityToken(
//                issuer: _config["Jwt:Issuer"],
//                audience: _config["Jwt:Audience"],
//                claims: claims,
//                expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"])),
//                signingCredentials: creds
//            );

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }
//    }
//}
