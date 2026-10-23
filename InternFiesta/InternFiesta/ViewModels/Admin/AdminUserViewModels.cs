
namespace InternFiesta.ViewModels.Admin
{
    public class AdminUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public List<string> Roles { get; set; } = new();
    }

    public class AdminUsersIndexViewModel
    {
        public string Search { get; set; } = string.Empty;

        public string SelectedRole { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();

        public List<AdminUserRowViewModel> Users
        { get; set; } = new();

        public int TotalUsers { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; } = 1;
    }

    public class AdminUserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}
