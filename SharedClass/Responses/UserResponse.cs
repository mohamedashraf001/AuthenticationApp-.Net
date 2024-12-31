namespace SharedClasses.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<RoleResponse> Roles { get; set; } = new List<RoleResponse>();

    }
}
