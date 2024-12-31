
using System.ComponentModel.DataAnnotations;
namespace BusinessLayer.Requests
{
    public class CreateRoleRequest
    {
       
        public string Name { get; set; }

        [Required]
    
        public List<int> Permissions { get; set; }
    }
}
