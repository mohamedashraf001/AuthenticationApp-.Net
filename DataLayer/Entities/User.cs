using DataAccessLayer.Data;
using System.Security.Claims;
using System.Text;

namespace DataAccessLayer.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Password { get; set; }
        public List<Role> Roles { get; set; } = new List<Role>();

        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();


    }
}
