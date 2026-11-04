using Microsoft.AspNetCore.Identity;

namespace Chores.Infrastructure.Identity;

/// <summary>The ASP.NET Identity user behind a Member: same id, username, email, password hash and lockout state.</summary>
public sealed class MemberCredential : IdentityUser<Guid>;
