using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string name) : base(name) { }

    public string Description { get; set; } = "";
}
